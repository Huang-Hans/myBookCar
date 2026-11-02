using LMS.BuildingBlocks.Core.Notifications.Delivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 长连接不会像 HTTP 请求那样每次重新认证，因此由守卫补上两件事：
// 1. 访问令牌到期即关闭连接，客户端须用新令牌重新连接；
// 2. 按间隔重新核验接收人授权（停用、权限或物业范围变化），不再有效即关闭连接并移出个人组。
// 授权数据源暂时不可用时连接保持到下个周期或令牌到期，不把暂时故障误判为撤销。
internal sealed class NotificationConnectionGuard(
    IServiceScopeFactory scopeFactory,
    NotificationConnectionRegistry registry,
    IOptions<NotificationDeliveryOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationConnectionGuard> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.GuardIntervalSeconds));
        while (await WaitAsync(timer, stoppingToken))
        {
            await SweepAsync(stoppingToken);
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task SweepAsync(CancellationToken stoppingToken)
    {
        foreach (var connection in registry.Snapshot())
        {
            await CheckAsync(connection, stoppingToken);
        }
    }

    private async Task CheckAsync(NotificationTrackedConnection connection, CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow();
        if (connection.IsExpiredAt(now))
        {
            Close(connection);
            return;
        }

        if (now - connection.VerifiedAt >= TimeSpan.FromSeconds(options.Value.ReverifySeconds))
        {
            await ReverifyAsync(connection, now, stoppingToken);
        }
    }

    private async Task ReverifyAsync(NotificationTrackedConnection connection, DateTimeOffset now, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var verifier = scope.ServiceProvider.GetService<INotificationConnectionVerifier>();
            if (verifier is null || await verifier.IsStillAuthorizedAsync(connection.Access, stoppingToken))
            {
                connection.VerifiedAt = now;
                return;
            }

            Close(connection);
        }
        catch (NotificationVerificationUnavailableException exception)
        {
            logger.LogWarning(exception, "Connection authorization could not be re-verified; keeping the connection until the next cycle.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Connection authorization re-verification failed unexpectedly.");
        }
    }

    private void Close(NotificationTrackedConnection connection)
    {
        registry.Remove(connection.Context.ConnectionId);
        connection.Context.Abort();
    }
}

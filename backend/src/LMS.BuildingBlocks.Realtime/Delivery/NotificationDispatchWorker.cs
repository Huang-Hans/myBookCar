using LMS.BuildingBlocks.Core.Notifications.Delivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Realtime.Delivery;

// 后台定时处理待发送事件。第一版每个承载服务单实例；租约保证多个进程同时运行时同一事件不会被同时处理，
// 但数据库租约不能替代 SignalR 的多实例分发，多实例部署需要另行引入 backplane。
internal sealed class NotificationDispatchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDeliveryOptions> options,
    ILogger<NotificationDispatchWorker> logger) : BackgroundService
{
    private readonly string leaseOwner = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.DispatcherEnabled)
        {
            logger.LogInformation("Notification dispatch worker is disabled by configuration.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollSeconds));
        while (await WaitAsync(timer, stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
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

    // 一个周期内持续处理到没有到期事件；单次失败（例如数据库暂时不可用）只记录，下个周期继续。
    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<NotificationDispatcher>();
            while (await dispatcher.RunOnceAsync(leaseOwner, stoppingToken) > 0)
            {
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Notification dispatch cycle failed; it will be retried on the next cycle.");
        }
    }
}

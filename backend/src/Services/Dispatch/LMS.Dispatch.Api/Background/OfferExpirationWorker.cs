using LMS.Dispatch.Application.Offers.Options;
using LMS.Dispatch.Application.Offers.Services;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Api.Background;

// 定时把已过期的待响应邀请写成 EXPIRED，释放它占用的行程与司机，使订单回到可重派状态。
// 多个实例同时运行是安全的：关闭以邀请版本为条件，输家得到“已被改变”而不会重复写入。
internal sealed class OfferExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OfferOptions> options,
    ILogger<OfferExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.ExpirySweepSeconds));
        using var timer = new PeriodicTimer(interval);
        while (await WaitAsync(timer, stoppingToken))
        {
            await SweepOnceAsync(stoppingToken);
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

    // 单次失败（例如数据库暂时不可用）只记录，下一个周期继续。
    private async Task SweepOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var closed = await scope.ServiceProvider.GetRequiredService<IOfferExpirationSweeper>().SweepAsync(stoppingToken);
            if (closed > 0)
            {
                logger.LogInformation("Expired {Count} unanswered driver offers.", closed);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Driver offer expiry sweep failed; it will be retried on the next cycle.");
        }
    }
}

using LMS.Reporting.Application.Refresh.Handlers;
using LMS.Reporting.Application.Refresh.Models;
using LMS.Reporting.Application.Refresh.Options;
using LMS.Reporting.Domain.Building;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Api.Workers;

// 报表行的定时重算：未启用时不做任何事。启动后若配置了回填区间，先回填一次，再每隔 IntervalMinutes 重算最近的业务日窗口。
// 单轮失败只记录日志并在 1 分钟后重试（例如启动时 Booking / Dispatch 尚未就绪），全部成功才按 IntervalMinutes 等待；
// 多实例同时运行时按业务日加锁，不会重复写入。
public sealed class ReportRowRefreshWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReportRefreshOptions> options,
    TimeProvider timeProvider,
    ILogger<ReportRowRefreshWorker> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            return;
        }

        if (settings.BackfillFrom is { } backfillFrom && settings.BackfillTo is { } backfillTo && backfillFrom <= backfillTo)
        {
            await RunOnceAsync(backfillFrom, backfillTo, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var today = BusinessDay.Of(timeProvider.GetUtcNow());
            var succeeded = await RunOnceAsync(today.AddDays(-settings.DaysBack), today.AddDays(settings.DaysAhead), stoppingToken);
            await DelayAsync(succeeded ? TimeSpan.FromMinutes(settings.IntervalMinutes) : RetryDelay, stoppingToken);
        }
    }

    // 返回本轮是否全部成功（没有因来源不可用而跳过的业务日，也没有异常）。
    private async Task<bool> RunOnceAsync(DateOnly from, DateOnly to, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<IRefreshReportRowsHandler>().RefreshAsync(from, to, stoppingToken);
            Log(from, to, result);

            return result.UnavailableDays == 0;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Report row refresh for {From}..{To} failed.", from, to);

            return false;
        }
    }

    private void Log(DateOnly from, DateOnly to, ReportRefreshResult result)
    {
        if (result.UnavailableDays > 0)
        {
            logger.LogWarning(
                "Report row refresh for {From}..{To}: {Refreshed} day(s) refreshed, {Unavailable} skipped because the {Source} source was unavailable, {Locked} locked by another instance.",
                from, to, result.RefreshedDays, result.UnavailableDays, result.UnavailableSource, result.LockedDays);
            return;
        }

        logger.LogInformation(
            "Report row refresh for {From}..{To}: {Refreshed} day(s) refreshed, {Locked} locked by another instance.",
            from, to, result.RefreshedDays, result.LockedDays);
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 服务正在停止，结束循环即可。
        }
    }
}

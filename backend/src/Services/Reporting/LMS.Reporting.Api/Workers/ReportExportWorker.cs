using LMS.Reporting.Application.Exports.Options;
using LMS.Reporting.Application.Exports.Services;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Api.Workers;

// 报表导出的后台执行者：轮询领取 QUEUED（或租约过期的 RUNNING）作业并执行。多个实例可同时运行，
// 领取是原子的。单个作业失败只影响该作业（已标记 FAILED），循环继续；服务停止时当前作业保留 RUNNING，租约过期后被重新领取。
public sealed class ReportExportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExportOptions> options,
    ILogger<ReportExportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await TryRunOnceAsync(stoppingToken);
            if (!processed)
            {
                await DelayAsync(stoppingToken);
            }
        }
    }

    private async Task<bool> TryRunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IReportExportRunner>().RunNextAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "A report export job failed.");

            return false;
        }
    }

    private async Task DelayAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.Value.PollSeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 服务正在停止，结束循环即可。
        }
    }
}

namespace LMS.Reporting.Application.Exports.Services;

public interface IReportExportRunner
{
    // 领取并执行一个待处理的报表导出作业；没有可执行的作业返回 false。
    Task<bool> RunNextAsync(CancellationToken cancellationToken);
}

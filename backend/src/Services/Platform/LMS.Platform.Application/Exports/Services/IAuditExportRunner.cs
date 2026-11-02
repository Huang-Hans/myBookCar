namespace LMS.Platform.Application.Exports.Services;

public interface IAuditExportRunner
{
    // 领取并执行一个待处理的审计导出作业；没有可执行的作业返回 false。
    Task<bool> RunNextAsync(CancellationToken cancellationToken);
}

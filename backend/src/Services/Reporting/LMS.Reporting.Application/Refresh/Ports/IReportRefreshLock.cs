namespace LMS.Reporting.Application.Refresh.Ports;

// 按业务日的租约锁：多实例部署时同一业务日同时只有一个实例在重算。租约到期后自动失效，持有者崩溃不会永久占用。
public interface IReportRefreshLock
{
    Task<bool> TryAcquireAsync(string key, string holder, TimeSpan lease, CancellationToken cancellationToken);

    Task ReleaseAsync(string key, string holder, CancellationToken cancellationToken);
}

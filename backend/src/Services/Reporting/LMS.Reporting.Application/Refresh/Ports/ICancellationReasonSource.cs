using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Application.Refresh.Ports;

// Platform 当前生效的取消原因清单。读取失败抛出 ReportSourceUnavailableException。
public interface ICancellationReasonSource
{
    Task<IReadOnlyList<CancellationReasonInfo>> ListAsync(CancellationToken cancellationToken);
}

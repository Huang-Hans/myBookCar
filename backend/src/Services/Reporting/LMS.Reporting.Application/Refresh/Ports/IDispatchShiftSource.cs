using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Application.Refresh.Ports;

// Dispatch 的班次来源。读取失败抛出 ReportSourceUnavailableException。
public interface IDispatchShiftSource
{
    // 业务日期闭区间 [from, to] 内的全部班次。
    Task<IReadOnlyList<ReportShiftFact>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

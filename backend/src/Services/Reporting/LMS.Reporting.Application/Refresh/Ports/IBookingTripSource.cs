using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Application.Refresh.Ports;

// Booking 的行程事实来源。读取失败（网络、超时、非成功响应）抛出 ReportSourceUnavailableException。
public interface IBookingTripSource
{
    // 计划接车时间落在 [from, to) 内的全部行程，内部按批读完。
    Task<IReadOnlyList<ReportTripFact>> ListAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}

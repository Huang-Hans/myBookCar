using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.ReportingSources.Ports;

// 报表来源的行程读取：按计划接车时间区间读取全部物业、全部状态的行程，以行程标识为游标顺序分批返回。
public interface IReportingTripRepository
{
    Task<IReadOnlyList<Trip>> ListByPickupAsync(DateTimeOffset from, DateTimeOffset to, string? afterTripId, int limit, CancellationToken cancellationToken);
}

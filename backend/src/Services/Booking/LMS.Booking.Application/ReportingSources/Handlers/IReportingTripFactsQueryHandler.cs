using LMS.Booking.Application.ReportingSources.Models;

namespace LMS.Booking.Application.ReportingSources.Handlers;

// Reporting 定时重算报表时读取的行程事实。只读，不做物业范围限制：调用者是受限的 Reporting 服务身份。
public interface IReportingTripFactsQueryHandler
{
    Task<ReportingTripFactPage> ListAsync(ReportingTripFactsQuery query, CancellationToken cancellationToken);
}

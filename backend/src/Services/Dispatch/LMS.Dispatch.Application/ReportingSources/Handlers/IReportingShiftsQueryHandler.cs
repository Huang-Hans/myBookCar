using LMS.Dispatch.Application.ReportingSources.Models;

namespace LMS.Dispatch.Application.ReportingSources.Handlers;

// Reporting 定时重算司机利用率时读取的班次。只读，覆盖全部物业：调用者是受限的 Reporting 服务身份。
public interface IReportingShiftsQueryHandler
{
    // 业务日期闭区间 [from, to] 内的全部班次。
    Task<IReadOnlyList<ReportingShift>> ListAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

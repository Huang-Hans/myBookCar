using LMS.BuildingBlocks.Core.Querying;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Schedules.Models;
using LMS.Reporting.Domain.Schedules;

namespace LMS.Reporting.Application.Schedules.Ports;

public interface IReportScheduleRepository
{
    Task<ReportSchedule?> FindAsync(string scheduleId, CancellationToken cancellationToken);

    Task<ReportSchedule?> FindAsync(string scheduleId, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<ReportSchedule>> ListAsync(ListReportSchedulesQuery query, CancellationToken cancellationToken);

    // 同一范围内邮箱（忽略大小写）+ 频率 + 报表类型重复时抛出 DuplicateRecordException。
    Task InsertAsync(ReportSchedule schedule, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string scheduleId, ITransactionContext context, CancellationToken cancellationToken);
}

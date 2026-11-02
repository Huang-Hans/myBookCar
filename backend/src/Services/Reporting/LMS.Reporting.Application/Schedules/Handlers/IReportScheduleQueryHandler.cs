using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Reporting.Application.Schedules.Models;
using LMS.Reporting.Domain.Schedules;

namespace LMS.Reporting.Application.Schedules.Handlers;

public interface IReportScheduleQueryHandler
{
    Task<OperationResult<PageResult<ReportSchedule>>> ListAsync(ListReportSchedulesQuery query, CancellationToken cancellationToken);
}

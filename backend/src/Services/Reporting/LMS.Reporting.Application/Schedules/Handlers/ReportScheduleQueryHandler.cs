using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Reporting.Application.Schedules.Models;
using LMS.Reporting.Application.Schedules.Ports;
using LMS.Reporting.Domain.Schedules;

namespace LMS.Reporting.Application.Schedules.Handlers;

internal sealed class ReportScheduleQueryHandler(IReportScheduleRepository schedules) : IReportScheduleQueryHandler
{
    public async Task<OperationResult<PageResult<ReportSchedule>>> ListAsync(
        ListReportSchedulesQuery query,
        CancellationToken cancellationToken) =>
        OperationResult<PageResult<ReportSchedule>>.Succeeded(await schedules.ListAsync(query, cancellationToken));
}

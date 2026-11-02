using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Schedules.Commands;
using LMS.Reporting.Domain.Schedules;

namespace LMS.Reporting.Application.Schedules.Handlers;

public interface ICreateReportScheduleHandler
{
    Task<OperationResult<ReportSchedule>> HandleAsync(CreateReportScheduleCommand command, CancellationToken cancellationToken);
}

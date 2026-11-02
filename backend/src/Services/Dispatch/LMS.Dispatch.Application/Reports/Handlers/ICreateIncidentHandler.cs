using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Reports.Commands;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Handlers;

public interface ICreateIncidentHandler
{
    Task<OperationResult<DriverIncident>> HandleAsync(CreateIncidentCommand command, CancellationToken cancellationToken);
}

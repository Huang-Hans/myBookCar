using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Handlers;

public interface IIncidentQueryHandler
{
    Task<OperationResult<PageResult<DriverIncident>>> ListAsync(ListIncidentsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<DriverIncident>> GetAsync(string incidentId, string driverId, CancellationToken cancellationToken);
}

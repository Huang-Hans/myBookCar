using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Application.Reports.Ports;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Handlers;

internal sealed class IncidentQueryHandler(IDriverIncidentRepository incidents) : IIncidentQueryHandler
{
    public async Task<OperationResult<PageResult<DriverIncident>>> ListAsync(ListIncidentsQuery query, CancellationToken cancellationToken) =>
        OperationResult<PageResult<DriverIncident>>.Succeeded(await incidents.ListForDriverAsync(query, cancellationToken));

    public async Task<OperationResult<DriverIncident>> GetAsync(string incidentId, string driverId, CancellationToken cancellationToken)
    {
        var incident = await incidents.FindForDriverAsync(incidentId, driverId, cancellationToken);

        return incident is null ? OperationResult<DriverIncident>.Failed(OperationOutcome.NotFound) : OperationResult<DriverIncident>.Succeeded(incident);
    }
}

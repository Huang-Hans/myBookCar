using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Ports;

public interface IDriverIncidentRepository
{
    Task<DriverIncident?> FindByIdAsync(string incidentId, CancellationToken cancellationToken);

    Task<DriverIncident?> FindForDriverAsync(string incidentId, string driverId, CancellationToken cancellationToken);

    Task<PageResult<DriverIncident>> ListForDriverAsync(ListIncidentsQuery query, CancellationToken cancellationToken);

    Task InsertAsync(DriverIncident incident, ITransactionContext context, CancellationToken cancellationToken);
}

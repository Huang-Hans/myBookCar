using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Ports;

public interface IVehicleIssueRepository
{
    Task<VehicleIssue?> FindByIdAsync(string issueId, CancellationToken cancellationToken);

    Task<VehicleIssue?> FindForDriverAsync(string issueId, string driverId, CancellationToken cancellationToken);

    Task<PageResult<VehicleIssue>> ListForDriverAsync(ListVehicleIssuesQuery query, CancellationToken cancellationToken);

    Task InsertAsync(VehicleIssue issue, ITransactionContext context, CancellationToken cancellationToken);
}

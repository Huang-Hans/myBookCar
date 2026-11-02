using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Handlers;

public interface IVehicleIssueQueryHandler
{
    Task<OperationResult<PageResult<VehicleIssue>>> ListAsync(ListVehicleIssuesQuery query, CancellationToken cancellationToken);

    Task<OperationResult<VehicleIssue>> GetAsync(string issueId, string driverId, CancellationToken cancellationToken);
}

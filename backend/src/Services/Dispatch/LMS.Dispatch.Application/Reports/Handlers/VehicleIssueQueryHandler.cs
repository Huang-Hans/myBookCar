using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Application.Reports.Ports;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Handlers;

internal sealed class VehicleIssueQueryHandler(IVehicleIssueRepository issues) : IVehicleIssueQueryHandler
{
    public async Task<OperationResult<PageResult<VehicleIssue>>> ListAsync(ListVehicleIssuesQuery query, CancellationToken cancellationToken) =>
        OperationResult<PageResult<VehicleIssue>>.Succeeded(await issues.ListForDriverAsync(query, cancellationToken));

    public async Task<OperationResult<VehicleIssue>> GetAsync(string issueId, string driverId, CancellationToken cancellationToken)
    {
        var issue = await issues.FindForDriverAsync(issueId, driverId, cancellationToken);

        return issue is null ? OperationResult<VehicleIssue>.Failed(OperationOutcome.NotFound) : OperationResult<VehicleIssue>.Succeeded(issue);
    }
}

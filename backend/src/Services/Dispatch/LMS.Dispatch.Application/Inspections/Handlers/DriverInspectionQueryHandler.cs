using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Inspections.Models;
using LMS.Dispatch.Application.Inspections.Ports;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Application.Inspections.Handlers;

internal sealed class DriverInspectionQueryHandler(IDriverInspectionRepository inspections) : IDriverInspectionQueryHandler
{
    public async Task<OperationResult<PageResult<DriverInspection>>> ListAsync(ListInspectionsQuery query, CancellationToken cancellationToken) =>
        OperationResult<PageResult<DriverInspection>>.Succeeded(await inspections.ListForDriverAsync(query, cancellationToken));

    public async Task<OperationResult<DriverInspection>> GetAsync(string inspectionId, string driverId, CancellationToken cancellationToken)
    {
        var inspection = await inspections.FindForDriverAsync(inspectionId, driverId, cancellationToken);

        return inspection is null
            ? OperationResult<DriverInspection>.Failed(OperationOutcome.NotFound)
            : OperationResult<DriverInspection>.Succeeded(inspection);
    }
}

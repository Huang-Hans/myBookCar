using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Inspections.Models;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Application.Inspections.Handlers;

public interface IDriverInspectionQueryHandler
{
    Task<OperationResult<PageResult<DriverInspection>>> ListAsync(ListInspectionsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<DriverInspection>> GetAsync(string inspectionId, string driverId, CancellationToken cancellationToken);
}

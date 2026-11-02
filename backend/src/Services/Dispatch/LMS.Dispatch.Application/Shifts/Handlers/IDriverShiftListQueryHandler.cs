using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Handlers;

public interface IDriverShiftListQueryHandler
{
    Task<OperationResult<PageResult<ShiftCard>>> ListAsync(
        string driverId,
        PropertyScope scope,
        ListQuery list,
        CancellationToken cancellationToken);
}

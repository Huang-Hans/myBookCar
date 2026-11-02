using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Handlers;

public interface IShiftBoardQueryHandler
{
    Task<OperationResult<ShiftBoard>> GetBoardAsync(ShiftBoardQuery query, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<ShiftBoard>>> GetCalendarAsync(ShiftCalendarQuery query, CancellationToken cancellationToken);

    Task<OperationResult<ShiftUpcoming>> GetUpcomingAsync(ShiftBoardQuery query, CancellationToken cancellationToken);

    Task<OperationResult<ShiftCard>> GetAsync(string shiftId, PropertyScope scope, CancellationToken cancellationToken);
}

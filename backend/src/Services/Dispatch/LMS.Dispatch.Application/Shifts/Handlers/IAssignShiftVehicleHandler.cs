using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Handlers;

public interface IAssignShiftVehicleHandler
{
    Task<OperationResult<ShiftCard>> AssignAsync(AssignShiftVehicleCommand command, CancellationToken cancellationToken);

    Task<OperationResult<ShiftCard>> ClearAsync(ClearShiftVehicleCommand command, CancellationToken cancellationToken);
}

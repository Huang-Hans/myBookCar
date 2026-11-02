using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Handlers;

public interface ICreateShiftHandler
{
    Task<OperationResult<ShiftCard>> HandleAsync(CreateShiftCommand command, CancellationToken cancellationToken);
}

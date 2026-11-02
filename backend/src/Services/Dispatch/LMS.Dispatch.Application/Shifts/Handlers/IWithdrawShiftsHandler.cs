using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Handlers;

public interface IWithdrawShiftsHandler
{
    Task<OperationResult<ShiftWithdrawalResult>> HandleAsync(WithdrawShiftsCommand command, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Domain.Shifts.Batches;

namespace LMS.Dispatch.Application.Shifts.Handlers;

public interface ICreateShiftBatchHandler
{
    Task<OperationResult<ShiftBatch>> HandleAsync(CreateShiftBatchCommand command, CancellationToken cancellationToken);
}

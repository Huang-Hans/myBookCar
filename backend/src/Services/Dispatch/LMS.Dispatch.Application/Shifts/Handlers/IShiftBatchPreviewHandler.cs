using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Handlers;

public interface IShiftBatchPreviewHandler
{
    Task<OperationResult<ShiftBatchPreview>> HandleAsync(ShiftBatchRules rules, PropertyScope scope, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Services;

namespace LMS.Dispatch.Application.Shifts.Handlers;

internal sealed class ShiftBatchPreviewHandler(ShiftBatchPlanner planner, ShiftBatchPropertyCheck properties) : IShiftBatchPreviewHandler
{
    public async Task<OperationResult<ShiftBatchPreview>> HandleAsync(ShiftBatchRules rules, PropertyScope scope, CancellationToken cancellationToken)
    {
        var failure = await properties.CheckAsync<ShiftBatchPreview>(rules, scope, cancellationToken);
        return failure ?? await planner.PlanAsync(rules, scope, null, cancellationToken);
    }
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Handlers;

// 调整角色（首选 / 待命）或到班标记（依时当班）。不改日期、时段和司机。
internal sealed class UpdateShiftHandler(
    PatchWorkflow workflow,
    IDriverShiftRepository shifts,
    ShiftCardComposer cards) : IUpdateShiftHandler
{
    private const string Operation = "shift.update";
    private const string ResourceType = "shift";

    public async Task<OperationResult<ShiftCard>> HandleAsync(UpdateShiftCommand command, CancellationToken cancellationToken)
    {
        var updated = await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
        if (!updated.IsSuccess)
        {
            return OperationResult<ShiftCard>.FailureOf(updated);
        }

        return ShiftResults.From(updated, await cards.ComposeAsync(updated.Value!, cancellationToken));
    }

    private PatchSpecification<DriverShift> BuildSpecification(UpdateShiftCommand command) =>
        new(
            command.ShiftId,
            command.ExpectedVersion,
            (id, context, token) => shifts.FindVisibleAsync(id, command.Context.Scope, context, token),
            shift => shift.Version,
            (shift, _, _) => Task.FromResult(OperationResult<DriverShift>.Succeeded(shift.Apply(command.Changes))),
            (updated, expected, context, token) => shifts.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.ShiftId, now, command.Changes.ChangedFields(), after.PropertyId));
}

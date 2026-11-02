using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Idempotency.Ports;
using LMS.Dispatch.Application.Idempotency.Services;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Handlers;

// Schedule a driver in a business date and period. Vehicle assignment is a separate step.
internal sealed class CreateShiftHandler(
    CreateWorkflow workflow,
    IDriverShiftRepository shifts,
    IDriverRepository drivers,
    IPlatformDirectory directory,
    IIdempotencyRecordRepository idempotencyRecords,
    ShiftCardComposer cards,
    TimeProvider timeProvider) : ICreateShiftHandler
{
    private const string Operation = "shift.create";
    private const string ResourceType = "shift";

    public async Task<OperationResult<ShiftCard>> HandleAsync(CreateShiftCommand command, CancellationToken cancellationToken)
    {
        DriverShift? candidate = null;
        if (!await IsReplayAsync(command, cancellationToken))
        {
            var prepared = await PrepareAsync(command, cancellationToken);
            if (prepared.Failure is not null)
            {
                return prepared.Failure;
            }

            candidate = prepared.Shift;
        }

        var created = await workflow.ExecuteAsync(BuildSpecification(command, candidate), cancellationToken);
        if (!created.IsSuccess)
        {
            return OperationResult<ShiftCard>.FailureOf(created);
        }

        var current = await shifts.FindByIdAsync(created.Value!.ShiftId, cancellationToken) ?? created.Value;
        return ShiftResults.From(created, await cards.ComposeAsync(current, cancellationToken));
    }

    // 相同幂等键的重放要回到已创建的班次。此时司机可能已停用，或这条班次已占住当天名额，都不能再按新建拒绝。
    private async Task<bool> IsReplayAsync(CreateShiftCommand command, CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprintFactory.Create(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm());

        return await idempotencyRecords.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken) is not null;
    }

    private async Task<(OperationResult<ShiftCard>? Failure, DriverShift? Shift)> PrepareAsync(
        CreateShiftCommand command,
        CancellationToken cancellationToken)
    {
        var driver = await drivers.FindVisibleAsync(command.DriverId, command.Context.Scope, cancellationToken);
        if (driver is null)
        {
            return (OperationResult<ShiftCard>.Invalid([new FieldIssue("driverId", "Unknown driver")]), null);
        }

        if (driver.Status != DriverStatus.Active)
        {
            return (OperationResult<ShiftCard>.Invalid([new FieldIssue("driverId", "Must be an active driver")]), null);
        }

        var property = await DriverPropertyCheck.CheckAsync<ShiftCard>(directory, command.Context.Scope, driver.PropertyId, cancellationToken);
        if (property is not null)
        {
            return (property, null);
        }

        if (await shifts.FindByDriverInPeriodAsync(command.DriverId, command.BusinessDate, command.Period, cancellationToken) is not null)
        {
            return (OperationResult<ShiftCard>.Invalid([new FieldIssue("driverId", "Driver already has a shift in this period on this business date")]), null);
        }

        return (null, DriverShift.Create(command.BusinessDate, command.Period, driver.DriverId, command.DutyStatus, command.Role)
            with { PropertyId = driver.PropertyId });
    }

    private CreateSpecification<DriverShift> BuildSpecification(CreateShiftCommand command, DriverShift? candidate) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(candidate!),
            shift => shift.ShiftId,
            (shift, context, token) => shifts.InsertAsync(shift, context, token),
            (id, token) => shifts.FindByIdAsync(id, token),
            (shift, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, shift.ShiftId, now, shift.PropertyId));
}

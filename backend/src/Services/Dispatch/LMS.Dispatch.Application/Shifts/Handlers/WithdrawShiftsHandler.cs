using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Handlers;

internal sealed class WithdrawShiftsHandler(ITransactionRunner transactions, IDriverShiftRepository shifts,
    IAuditRecordRepository audits, ShiftWithdrawalGuard guard, TimeProvider time) : IWithdrawShiftsHandler
{
    public async Task<OperationResult<ShiftWithdrawalResult>> HandleAsync(WithdrawShiftsCommand command,
        CancellationToken cancellationToken)
    {
        if (!AllowsProperty(command))
            return OperationResult<ShiftWithdrawalResult>.Failed(OperationOutcome.Forbidden);
        if (!await shifts.SupportsWithdrawalAsync(cancellationToken))
            return new(OperationOutcome.SourceUnavailable, null,
                [new("scheduling", "Shift withdrawal requires a database update. Contact an administrator before retrying.")]);
        return await ExecuteAsync(command, cancellationToken);
    }

    private static bool AllowsProperty(WithdrawShiftsCommand command) =>
        command.PropertyId is not { } property || command.Context.Scope.Allows(property);

    private async Task<OperationResult<ShiftWithdrawalResult>> ExecuteAsync(WithdrawShiftsCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await transactions.ExecuteAsync((context, token) => CommitAsync(command, context, token), cancellationToken);
        }
        catch (ShiftWithdrawalConflictException)
        {
            return ShiftWithdrawalGuard.Conflict();
        }
    }

    private async Task<OperationResult<ShiftWithdrawalResult>> CommitAsync(WithdrawShiftsCommand command,
        ITransactionContext context, CancellationToken cancellationToken)
    {
        var targets = await shifts.ListWithdrawalTargetsAsync(command.Targets.Select(target => target.ShiftId).ToList(),
            command.Context.Scope, context, cancellationToken);
        var rejection = await guard.CheckAsync(command, targets, context, cancellationToken);
        if (rejection is not null) return rejection;
        var active = targets.Where(shift => shift.WithdrawnAt is null).ToList();
        if (active.Count == 0) return OperationResult<ShiftWithdrawalResult>.Succeeded(new(command.BusinessDate, command.Period, 0));
        return await WriteAsync(command, active, context, cancellationToken);
    }

    private async Task<OperationResult<ShiftWithdrawalResult>> WriteAsync(WithdrawShiftsCommand command,
        IReadOnlyList<DriverShift> active, ITransactionContext context, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        if (!await shifts.TryWithdrawManyAsync(active, command.Context.Actor, now, context, cancellationToken))
            throw new ShiftWithdrawalConflictException();
        var records = active.Select(shift => AuditRecordFactory.Succeeded(command.Context,
            command.WholePeriod ? "shift.period-withdraw" : "shift.withdraw", "shift", shift.ShiftId, now,
            "withdrawn; released driver and vehicle occupancy", shift.PropertyId)).ToList();
        await audits.InsertManyAsync(records, context, cancellationToken);
        return OperationResult<ShiftWithdrawalResult>.Succeeded(new(command.BusinessDate, command.Period, active.Count));
    }
}

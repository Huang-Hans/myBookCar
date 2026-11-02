using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Services;

internal sealed class ShiftWithdrawalGuard(IDriverShiftRepository shifts)
{
    public async Task<OperationResult<ShiftWithdrawalResult>?> CheckAsync(WithdrawShiftsCommand command,
        IReadOnlyList<DriverShift> targets, ITransactionContext context, CancellationToken cancellationToken)
    {
        var rejection = CheckTargets(command, targets);
        if (rejection is not null) return rejection;
        var active = targets.Where(shift => shift.WithdrawnAt is null).ToList();
        return await CheckPeriodIfNeededAsync(command, active, context, cancellationToken);
    }

    private static OperationResult<ShiftWithdrawalResult>? CheckTargets(WithdrawShiftsCommand command,
        IReadOnlyList<DriverShift> targets)
    {
        if (targets.Count != command.Targets.Count)
            return OperationResult<ShiftWithdrawalResult>.Failed(OperationOutcome.NotFound);
        if (targets.Any(shift => !MatchesScope(command, shift))) return Conflict();
        return CheckVersions(command, targets);
    }

    private static OperationResult<ShiftWithdrawalResult>? CheckVersions(WithdrawShiftsCommand command,
        IReadOnlyList<DriverShift> targets)
    {
        var versions = command.Targets.ToDictionary(target => target.ShiftId, target => target.Version);
        var active = targets.Where(shift => shift.WithdrawnAt is null).ToList();
        return active.Any(shift => shift.Version != versions[shift.ShiftId]) ? Conflict() : null;
    }

    private async Task<OperationResult<ShiftWithdrawalResult>?> CheckPeriodIfNeededAsync(WithdrawShiftsCommand command,
        IReadOnlyList<DriverShift> active, ITransactionContext context, CancellationToken cancellationToken)
    {
        if (!command.WholePeriod || active.Count == 0) return null;
        return await CheckPeriodAsync(command, active, context, cancellationToken);
    }

    private async Task<OperationResult<ShiftWithdrawalResult>?> CheckPeriodAsync(WithdrawShiftsCommand command,
        IReadOnlyList<DriverShift> active, ITransactionContext context, CancellationToken cancellationToken)
    {
        var current = await shifts.ListActivePeriodAsync(command.BusinessDate, command.Period, command.Context.Scope,
            command.PropertyId, context, cancellationToken);
        return current.Select(shift => shift.ShiftId).ToHashSet(StringComparer.Ordinal)
            .SetEquals(active.Select(shift => shift.ShiftId)) ? null : Conflict();
    }

    private static bool MatchesScope(WithdrawShiftsCommand command, DriverShift shift) =>
        MatchesPeriod(command, shift) && MatchesProperty(command, shift);

    private static bool MatchesPeriod(WithdrawShiftsCommand command, DriverShift shift) =>
        shift.BusinessDate == command.BusinessDate && shift.Period == command.Period;

    private static bool MatchesProperty(WithdrawShiftsCommand command, DriverShift shift) =>
        command.PropertyId is null || shift.PropertyId == command.PropertyId;

    public static OperationResult<ShiftWithdrawalResult> Conflict() =>
        new(OperationOutcome.StateConflict, null,
            [new("shifts", "The roster has changed. Refresh and confirm the withdrawal again.")]);
}

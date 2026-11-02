using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Commands;

public sealed record WithdrawShiftsCommand(DateOnly BusinessDate, ShiftPeriod Period, string? PropertyId, bool WholePeriod,
    IReadOnlyList<ShiftWithdrawalTarget> Targets, CommandContext Context);

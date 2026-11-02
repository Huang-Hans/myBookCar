namespace LMS.Dispatch.Api.Contracts.Shifts;

public sealed record WithdrawShiftsRequest(string? BusinessDate, string? Period, string? PropertyId, bool? IsWholePeriod,
    IReadOnlyList<ShiftWithdrawalTargetRequest?>? Targets);

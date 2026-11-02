namespace LMS.Dispatch.Api.Contracts.Shifts;

public sealed record ShiftWithdrawalTargetRequest(string? ShiftId, long? Version);

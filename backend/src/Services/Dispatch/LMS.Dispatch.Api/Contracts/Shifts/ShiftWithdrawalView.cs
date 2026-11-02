namespace LMS.Dispatch.Api.Contracts.Shifts;

public sealed record ShiftWithdrawalView(string BusinessDate, string Period, int WithdrawnCount);

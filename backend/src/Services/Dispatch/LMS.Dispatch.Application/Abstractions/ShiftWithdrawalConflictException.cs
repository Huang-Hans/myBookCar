namespace LMS.Dispatch.Application.Abstractions;

// Throw after a failed conditional bulk write so the transaction cannot commit partial changes.
public sealed class ShiftWithdrawalConflictException : Exception;

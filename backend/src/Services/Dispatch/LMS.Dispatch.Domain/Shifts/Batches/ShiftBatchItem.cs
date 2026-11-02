namespace LMS.Dispatch.Domain.Shifts.Batches;

public sealed record ShiftBatchItem(
    DateOnly BusinessDate,
    string DriverId,
    string DriverName,
    bool IsExisting,
    ShiftPeriod? Period,
    ShiftDutyStatus? DutyStatus,
    ShiftRole? Role,
    string? ShiftId,
    string? PropertyId);

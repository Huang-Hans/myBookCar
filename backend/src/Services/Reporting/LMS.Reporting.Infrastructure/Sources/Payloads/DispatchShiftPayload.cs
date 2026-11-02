namespace LMS.Reporting.Infrastructure.Sources.Payloads;

// Dispatch GET internal/v1/reporting/shifts 的一条班次。
internal sealed record DispatchShiftPayload(
    string ShiftId,
    string BusinessDate,
    string Period,
    string DriverId,
    string? DriverName,
    string PropertyId,
    string DutyStatus);

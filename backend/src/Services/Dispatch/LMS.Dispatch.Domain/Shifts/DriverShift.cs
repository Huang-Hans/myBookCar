using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Domain.Shifts;

// A driver's shift. Property and vehicle are current driver projections, never persisted with the shift.
public sealed record DriverShift(
    string ShiftId,
    DateOnly BusinessDate,
    ShiftPeriod Period,
    string DriverId,
    ShiftDutyStatus DutyStatus,
    ShiftRole Role,
    ShiftAttendance Attendance,
    long Version)
{
    public string PropertyId { get; init; } = string.Empty;

    public string? VehiclePlate { get; init; }

    public VehicleGrade? VehicleGrade { get; init; }

    private const string IdPrefix = "shift_";
    private const long FirstVersion = 1;

    public DateTimeOffset? WithdrawnAt { get; init; }
    public string? WithdrawnBy { get; init; }

    public static DriverShift Create(
        DateOnly businessDate,
        ShiftPeriod period,
        string driverId,
        ShiftDutyStatus dutyStatus,
        ShiftRole role) =>
        new(IdPrefix + Guid.CreateVersion7().ToString("N"), businessDate, period, driverId, dutyStatus, role, ShiftAttendance.Scheduled, FirstVersion);

    public DriverShift Apply(ShiftChanges changes) => this with
    {
        DutyStatus = changes.DutyStatus.ValueOr(DutyStatus),
        Role = changes.Role.ValueOr(Role),
        Attendance = changes.Attendance.ValueOr(Attendance)
    };

}

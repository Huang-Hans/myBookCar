namespace LMS.Dispatch.Domain.Shifts;

public static class ShiftBadges
{
    public static IReadOnlyList<ShiftBadge> For(DriverShift shift)
    {
        var badges = new List<ShiftBadge> { DutyBadge(shift.DutyStatus) };

        var role = shift.Role == ShiftRole.Preferred ? ShiftBadge.Preferred : ShiftBadge.Standby;
        if (!badges.Contains(role))
        {
            badges.Add(role);
        }

        if (shift.Attendance == ShiftAttendance.OnTime)
        {
            badges.Add(ShiftBadge.OnTime);
        }

        if (shift.VehiclePlate is not null)
        {
            badges.Add(ShiftBadge.VehicleAssigned);
        }

        return badges;
    }

    private static ShiftBadge DutyBadge(ShiftDutyStatus status) => status switch
    {
        ShiftDutyStatus.OnDuty => ShiftBadge.OnDuty,
        ShiftDutyStatus.Rest => ShiftBadge.Rest,
        ShiftDutyStatus.Leave => ShiftBadge.Leave,
        _ => throw new InvalidOperationException($"Unknown duty status '{status}'.")
    };
}

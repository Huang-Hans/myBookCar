namespace LMS.Dispatch.Domain.Shifts;

// PREFERRED and STANDBY are mutually exclusive roles within each individual shift.
public enum ShiftRole
{
    Preferred,
    Standby
}

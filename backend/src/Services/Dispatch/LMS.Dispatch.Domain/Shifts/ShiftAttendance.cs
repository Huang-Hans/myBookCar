namespace LMS.Dispatch.Domain.Shifts;

// 到班标记。SCHEDULED 是排班时的默认，画面上不另挂徽章；ON_TIME 对应「依时当班」。
public enum ShiftAttendance
{
    Scheduled,
    OnTime
}

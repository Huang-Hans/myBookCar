using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Domain.Shifts;

// 班次的局部更新。业务日期、时段、司机与物业在建立后不变；改期或换人应取消后重建。
public sealed record ShiftChanges(Optional<ShiftDutyStatus> DutyStatus, Optional<ShiftRole> Role, Optional<ShiftAttendance> Attendance)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(("status", DutyStatus.IsSet), ("role", Role.IsSet), ("attendance", Attendance.IsSet));
}

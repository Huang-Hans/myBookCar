using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// 部分更新班次。至少出现一个字段。日期、时段和司机不能在此修改。
/// </summary>
/// <param name="Status">状态字面值，取 ON_DUTY / REST / LEAVE，对应「当值 / 休息 / 请假」。未传保持原值。</param>
/// <param name="Role">角色字面值，取 PREFERRED / STANDBY。未传保持原值。</param>
/// <param name="Attendance">到班标记，取 SCHEDULED / ON_TIME。ON_TIME 对应「依时当班」。未传保持原值。</param>
public sealed record PatchShiftRequest(Optional<string?> Status, Optional<string?> Role, Optional<string?> Attendance);

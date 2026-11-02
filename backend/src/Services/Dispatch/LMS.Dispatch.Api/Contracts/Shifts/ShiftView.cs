namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// 一条班次。badges 由角色、到班标记和是否已派车推得，供卡片直接渲染。
/// </summary>
/// <param name="ShiftId">班次标识。</param>
/// <param name="BusinessDate">业务日期，YYYY-MM-DD。</param>
/// <param name="Period">时段字面值，取 MORNING / AFTERNOON / NIGHT。</param>
/// <param name="StartsAt">开始时间，HH:mm。</param>
/// <param name="EndsAt">结束时间，HH:mm。</param>
/// <param name="EndsNextDay">结束时间是否落在下一个日历日。</param>
/// <param name="DriverId">司机标识。</param>
/// <param name="DriverName">司机姓名。主档已不存在时省略。</param>
/// <param name="PropertyId">Current driver property, resolved at query time and never stored on the shift.</param>
/// <param name="VehiclePlate">Current driver's bound vehicle plate, read from vehicles by ID. Never stored on the shift; omitted if unbound.</param>
/// <param name="Status">状态字面值，取 ON_DUTY / REST / LEAVE，对应「当值 / 休息 / 请假」。</param>
/// <param name="Role">角色字面值，取 PREFERRED / STANDBY。</param>
/// <param name="Attendance">到班标记，取 SCHEDULED / ON_TIME。</param>
/// <param name="Badges">卡片徽章，取 ON_DUTY / REST / LEAVE / PREFERRED / STANDBY / ON_TIME / VEHICLE_ASSIGNED。</param>
/// <param name="VehicleGrade">Current eligible bound vehicle grade; null when unbound, in another property or out of service.</param>
/// <param name="Version">乐观锁版本号。当前暂不比较 X-Expected-Version。</param>
public sealed record ShiftView(
    string ShiftId,
    string BusinessDate,
    string Period,
    string StartsAt,
    string EndsAt,
    bool EndsNextDay,
    string DriverId,
    string? DriverName,
    string PropertyId,
    string? VehiclePlate,
    string Status,
    string Role,
    string Attendance,
    IReadOnlyList<string> Badges,
    long Version,
    string? VehicleGrade = null);

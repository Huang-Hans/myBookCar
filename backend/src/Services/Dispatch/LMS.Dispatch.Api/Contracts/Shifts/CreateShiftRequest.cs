namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// Create a shift. Property follows the current driver record; vehicle assignment is a separate step.
/// </summary>
/// <param name="BusinessDate">业务日期，YYYY-MM-DD。夜班记在开始那天。</param>
/// <param name="DriverId">Visible active driver without an existing shift in the same date and period.</param>
/// <param name="Period">班次字面值，取 MORNING / AFTERNOON / NIGHT，对应早班、午班、夜班。</param>
/// <param name="Status">状态字面值，取 ON_DUTY / REST / LEAVE，对应「当值 / 休息 / 请假」。省略时按当值。</param>
/// <param name="Role">角色字面值，取 PREFERRED / STANDBY。表单未单独给出时省略，按 PREFERRED（首选）。</param>
public sealed record CreateShiftRequest(
    string? BusinessDate,
    string? DriverId,
    string? Period,
    string? Status,
    string? Role);

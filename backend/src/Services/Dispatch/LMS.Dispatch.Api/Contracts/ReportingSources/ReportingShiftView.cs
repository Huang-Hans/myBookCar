namespace LMS.Dispatch.Api.Contracts.ReportingSources;

/// <summary>
/// 报表来源的一条班次。
/// </summary>
/// <param name="ShiftId">班次标识。</param>
/// <param name="BusinessDate">业务日期，YYYY-MM-DD；夜班挂在开始那天。</param>
/// <param name="Period">MORNING / AFTERNOON / NIGHT。</param>
/// <param name="DriverId">司机标识。</param>
/// <param name="DriverName">司机当前姓名；司机主档已不存在时为 null。</param>
/// <param name="PropertyId">Current driver property resolved when reading the reporting source.</param>
/// <param name="DutyStatus">ON_DUTY / REST / LEAVE。</param>
public sealed record ReportingShiftView(string ShiftId, string BusinessDate, string Period, string DriverId, string? DriverName, string PropertyId, string DutyStatus);

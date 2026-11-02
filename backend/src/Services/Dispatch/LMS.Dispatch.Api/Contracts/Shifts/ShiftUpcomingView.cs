namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// 从当天起连续 15 个业务日期的排班。没有班次的日期也返回，shifts 为空。
/// </summary>
/// <param name="TimeZone">业务时区标识，默认 Asia/Macau。</param>
/// <param name="From">第一天，含当天，YYYY-MM-DD。</param>
/// <param name="To">最后一天，含当天起第 15 天，YYYY-MM-DD。</param>
/// <param name="PropertyId">生效的物业筛选；未筛选时省略。</param>
/// <param name="Days">按日期升序的 15 天。</param>
public sealed record ShiftUpcomingView(
    string TimeZone,
    string From,
    string To,
    string? PropertyId,
    IReadOnlyList<ShiftUpcomingDayView> Days);

/// <summary>
/// 未来列表中的一天。
/// </summary>
/// <param name="BusinessDate">业务日期，YYYY-MM-DD。夜班仍归开始那天。</param>
/// <param name="Count">当天班次数。</param>
/// <param name="Shifts">当天班次。先按时段（早、午、夜），再把首选排在待命之前。</param>
public sealed record ShiftUpcomingDayView(string BusinessDate, int Count, IReadOnlyList<ShiftView> Shifts);

namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// 看板上的一个时段列。
/// </summary>
/// <param name="Period">时段字面值，取 MORNING / AFTERNOON / NIGHT。</param>
/// <param name="StartsAt">开始时间，HH:mm，按业务时区。</param>
/// <param name="EndsAt">结束时间，HH:mm。夜班为次日 07:00。</param>
/// <param name="EndsNextDay">结束时间是否落在下一个日历日。夜班为 true。</param>
/// <param name="Count">该列班次数。</param>
/// <param name="Shifts">班次卡片。首选在前，同一角色内按司机姓名排序。</param>
public sealed record ShiftColumnView(
    string Period,
    string StartsAt,
    string EndsAt,
    bool EndsNextDay,
    int Count,
    IReadOnlyList<ShiftView> Shifts);

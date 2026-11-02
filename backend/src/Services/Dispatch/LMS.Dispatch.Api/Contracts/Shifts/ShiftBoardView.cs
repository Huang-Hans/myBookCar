namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// Shift board: date queries return all three periods; pickupAt queries return only the matching on-duty period.
/// </summary>
/// <param name="TimeZone">业务时区标识，默认 Asia/Macau。</param>
/// <param name="BusinessDate">Shift start date, YYYY-MM-DD. Before 07:00 a pickup query resolves to the previous date.</param>
/// <param name="PropertyId">生效的物业筛选；未筛选时省略。</param>
/// <param name="Periods">All three periods for a date query, or the single matching period for a pickupAt query.</param>
public sealed record ShiftBoardView(
    string TimeZone,
    string BusinessDate,
    string? PropertyId,
    IReadOnlyList<ShiftColumnView> Periods);

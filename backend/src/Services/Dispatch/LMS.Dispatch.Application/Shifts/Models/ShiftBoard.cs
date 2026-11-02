using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Models;

public sealed record ShiftCard(DriverShift Shift, string? DriverName, IReadOnlyList<ShiftBadge> Badges);

public sealed record ShiftColumn(ShiftPeriod Period, ShiftWindow Window, IReadOnlyList<ShiftCard> Shifts);

public sealed record ShiftBoard(string TimeZoneId, DateOnly BusinessDate, string? PropertyId, IReadOnlyList<ShiftColumn> Periods);

public sealed record ShiftDay(DateOnly BusinessDate, IReadOnlyList<ShiftCard> Shifts);

// 从澳门业务当天起、含当天的连续排班。长度固定为 15 个业务日期。
public sealed record ShiftUpcoming(string TimeZoneId, DateOnly From, DateOnly To, string? PropertyId, IReadOnlyList<ShiftDay> Days);

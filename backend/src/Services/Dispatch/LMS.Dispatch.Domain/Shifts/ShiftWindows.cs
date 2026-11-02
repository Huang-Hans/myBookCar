namespace LMS.Dispatch.Domain.Shifts;

// 澳门业务日的固定班次窗口。夜班结束于次日 07:00，与次日早班相接、不重叠。
public static class ShiftWindows
{
    public static ShiftWindow Of(ShiftPeriod period) => period switch
    {
        ShiftPeriod.Morning => new ShiftWindow(new TimeOnly(7, 0), new TimeOnly(15, 0), false),
        ShiftPeriod.Afternoon => new ShiftWindow(new TimeOnly(15, 0), new TimeOnly(23, 0), false),
        ShiftPeriod.Night => new ShiftWindow(new TimeOnly(23, 0), new TimeOnly(7, 0), true),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };
}

public readonly record struct ShiftWindow(TimeOnly StartsAt, TimeOnly EndsAt, bool EndsNextDay);

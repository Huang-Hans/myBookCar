namespace LMS.Dispatch.Application.Shifts.Models;

public static class DriverShiftSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["businessDate", "propertyId", "period", "shiftId"];

    public const string DefaultSort = "businessDate,shiftId";
    public const string StableField = "shiftId";
}

namespace LMS.Dispatch.Application.Drivers.Models;

public static class DispatchCandidateSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["name", "driverId"];

    public const string DefaultSort = "name,driverId";
    public const string StableField = "driverId";
}

namespace LMS.Dispatch.Application.Inspections.Models;

public static class InspectionSorting
{
    public static IReadOnlyList<string> Fields { get; } = ["submittedAt", "phase", "inspectionId"];

    public const string DefaultSort = "-submittedAt,inspectionId";
    public const string StableField = "inspectionId";
}

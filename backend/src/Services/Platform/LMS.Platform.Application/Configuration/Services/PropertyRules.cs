using System.Text.RegularExpressions;
using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Services;

internal static partial class PropertyRules
{
    public static IReadOnlyList<FieldIssue> Validate(ConfiguredProperty property)
    {
        var issues = new List<FieldIssue>();
        if (!PropertyCode().IsMatch(property.PropertyId))
            issues.Add(new("propertyId", "Use 1 to 32 uppercase letters, digits, underscores or hyphens"));
        AddNonNegativeIssue(issues, "assignedFleetCount", property.AssignedFleetCount);
        AddNonNegativeIssue(issues, "minimumLeadTimeHours", property.MinimumLeadTimeHours);
        return issues;
    }

    private static void AddNonNegativeIssue(List<FieldIssue> issues, string field, int value)
    {
        if (value < 0) issues.Add(new(field, "Must be greater than or equal to 0"));
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9_-]{0,31}$")]
    private static partial Regex PropertyCode();
}

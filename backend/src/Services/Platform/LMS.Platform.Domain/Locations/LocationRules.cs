using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Platform.Domain.Locations;

// 合并后的完整地点必须满足的数值范围（合同 3.2.2、3.2.3）。
public static class LocationRules
{
    public const double MinRadiusMeters = 10;
    public const double MaxRadiusMeters = 5000;

    public static IReadOnlyList<FieldIssue> Validate(CommonLocation location)
    {
        var issues = new List<FieldIssue>();
        AddIfOutOfRange(issues, "coordinates.latitude", location.Coordinates.Latitude, -90, 90);
        AddIfOutOfRange(issues, "coordinates.longitude", location.Coordinates.Longitude, -180, 180);
        AddIfOutOfRange(issues, "geofence.radiusMeters", location.Geofence.RadiusMeters, MinRadiusMeters, MaxRadiusMeters);

        return issues;
    }

    private static void AddIfOutOfRange(List<FieldIssue> issues, string field, double value, double min, double max)
    {
        if (!double.IsFinite(value) || value < min || value > max)
        {
            issues.Add(new FieldIssue(field, $"Must be between {min} and {max}"));
        }
    }
}

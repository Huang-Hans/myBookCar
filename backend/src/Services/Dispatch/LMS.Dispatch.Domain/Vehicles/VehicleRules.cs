using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Dispatch.Domain.Vehicles;

public static class VehicleRules
{
    public static IReadOnlyList<FieldIssue> Validate(Vehicle vehicle)
    {
        var issues = new List<FieldIssue>();
        if (vehicle.Seats < 1)
        {
            issues.Add(new FieldIssue("seats", "Must be a positive integer"));
        }

        if (vehicle.MileageKm < 0)
        {
            issues.Add(new FieldIssue("mileageKm", "Must be zero or greater"));
        }

        return issues;
    }
}

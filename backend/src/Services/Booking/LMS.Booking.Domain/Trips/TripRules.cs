using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Domain.Trips;

// 行程单字段规则。资格、黑名单与配额等外部前置条件在应用层校验链（TripEligibilityService）中处理。
public static class TripRules
{
    // 容量与时间窗：乘客/行李不得超过所选车型容量；SCHEDULED/TODAY 不得早于最短提前时间。
    public static IReadOnlyList<FieldIssue> Validate(
        VehicleClass vehicleClass,
        int passengers,
        int luggage,
        DateTimeOffset pickupAt,
        bool enforceLeadTime,
        DateTimeOffset now,
        int minimumLeadTimeMinutes,
        VehicleCapacity capacity)
    {
        var issues = new List<FieldIssue>();

        if (passengers < 1 || passengers > capacity.MaxPassengers)
        {
            issues.Add(new FieldIssue("passengers", $"Must be between 1 and {capacity.MaxPassengers} for the selected vehicle class"));
        }

        if (luggage < 0 || luggage > capacity.MaxLuggage)
        {
            issues.Add(new FieldIssue("luggage", $"Must be between 0 and {capacity.MaxLuggage} for the selected vehicle class"));
        }

        if (enforceLeadTime && pickupAt < now.AddMinutes(minimumLeadTimeMinutes))
        {
            issues.Add(new FieldIssue("pickupAt", $"Must be at least {minimumLeadTimeMinutes} minutes ahead"));
        }

        return issues;
    }
}

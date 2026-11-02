using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Previews.Models;

namespace LMS.Booking.Application.Previews.Services;

// 把校验链的字段问题归类成稳定的原因代码，前端据此分支，不必解析英文文案。
internal static class BookingPreviewReasons
{
    private const string Generic = "RULE_VIOLATION";

    private static readonly IReadOnlyDictionary<string, string> CodesByField = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["guest"] = "GUEST_RESTRICTED",
        ["qualification"] = "QUALIFICATION_NOT_MET",
        ["vehicleClass"] = "VEHICLE_NOT_ENTITLED",
        ["passengers"] = "PASSENGERS_EXCEED_CAPACITY",
        ["luggage"] = "LUGGAGE_EXCEEDS_CAPACITY",
        ["pickupAt"] = "PICKUP_TIME_INVALID",
        ["quota"] = "QUOTA_EXHAUSTED",
        ["route.pickupLocationId"] = "LOCATION_NOT_ACTIVE",
        ["route.dropoffLocationId"] = "LOCATION_NOT_ACTIVE"
    };

    public static BookingPreviewReason From(FieldIssue issue) =>
        new(CodesByField.GetValueOrDefault(issue.Field, Generic), issue.Field, issue.Message);
}

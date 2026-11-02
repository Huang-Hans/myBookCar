using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Reports.Models;

// 各报表的排序约定：允许字段、默认排序（逗号分隔，- 为降序）与末尾补充的稳定标识。
public sealed record ReportSorting(IReadOnlyList<string> Fields, string DefaultSort)
{
    public const string StableField = "rowId";

    public static ReportSorting Of(ReportType type) => type switch
    {
        ReportType.DailyJobSummary => new(["propertyId", "bookings", "completed", "cancellations", "noShows", "redemptionSummary", "rowId"], "propertyId,rowId"),
        ReportType.TripDetail => new(
            [
                "tripReference", "propertyId", "guestName", "scheduledAt", "actualAt", "driverName", "vehicleDescription", "distanceMeters",
                "durationSeconds", "loyalty.redemptionId", "loyalty.pointsRequired", "loyalty.entitlement", "loyalty.redemptionState", "source", "rowId"
            ],
            "tripReference,rowId"),
        ReportType.DriverUtilization => new(
            ["driverName", "propertyId", "tripCount", "idleSeconds", "overtimeSeconds", "redemptionContributionReference", "rowId"], "driverName,rowId"),
        ReportType.VehicleUtilization => new(
            ["vehicleDescription", "mileageMeters", "tripCount", "downtimeSeconds", "redemptionReference", "rowId"], "vehicleDescription,rowId"),
        ReportType.Cancellation => new(
            ["reasonCode", "reason", "source", "noticeSecondsBeforePickup", "pointsReturnPolicyCode", "pointsReturnReference", "rowId"], "reasonCode,rowId"),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type.")
    };
}

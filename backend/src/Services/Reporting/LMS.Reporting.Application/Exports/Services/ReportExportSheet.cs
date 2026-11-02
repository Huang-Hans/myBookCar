using System.Globalization;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Exports.Services;

// 各报表导出的工作表：列与预览一致，时间为 UTC 的 ISO 8601，数值按不变区域性输出，缺失值留空而不是填演示值。
internal static class ReportExportSheet
{
    private const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public static string SheetNameOf(ReportType type) => type switch
    {
        ReportType.DailyJobSummary => "Daily job summary",
        ReportType.TripDetail => "Trip details",
        ReportType.DriverUtilization => "Driver utilization",
        ReportType.VehicleUtilization => "Vehicle utilization",
        ReportType.Cancellation => "Cancellations",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type.")
    };

    public static IReadOnlyList<string> HeadersOf(ReportType type) => type switch
    {
        ReportType.DailyJobSummary => ["Property", "Bookings", "Completed", "Cancellations", "No-shows", "Redemption summary", "Updated at (UTC)"],
        ReportType.TripDetail =>
        [
            "Trip reference", "Property", "Guest", "Scheduled (UTC)", "Actual (UTC)", "Driver ID", "Driver", "Vehicle", "Distance (m)",
            "Duration (s)", "Redemption ID", "Points required", "Entitlement", "Redemption state", "Source", "Updated at (UTC)"
        ],
        ReportType.DriverUtilization =>
            ["Driver ID", "Driver", "Property", "Trips", "Idle (s)", "Overtime (s)", "Redemption contribution reference", "Updated at (UTC)"],
        ReportType.VehicleUtilization => ["Vehicle", "Mileage (m)", "Trips", "Downtime (s)", "Redemption reference", "Updated at (UTC)"],
        ReportType.Cancellation =>
            ["Reason code", "Reason", "Source", "Notice before pickup (s)", "Points return policy", "Points return reference", "Updated at (UTC)"],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type.")
    };

    public static IReadOnlyList<string> RowOf(ReportRow row) => row.Payload switch
    {
        DailyJobSummaryPayload daily =>
        [
            daily.PropertyId, Number(daily.Bookings), Number(daily.Completed), Number(daily.Cancellations), Number(daily.NoShows),
            daily.RedemptionSummary ?? string.Empty, Instant(row.UpdatedAt)
        ],
        TripDetailPayload trip =>
        [
            trip.TripReference, trip.PropertyId, trip.GuestName, Instant(trip.ScheduledAt), Instant(trip.ActualAt), trip.DriverId ?? string.Empty,
            trip.DriverName ?? string.Empty, trip.VehicleDescription ?? string.Empty, Number(trip.DistanceMeters), Number(trip.DurationSeconds),
            trip.Loyalty.RedemptionId ?? string.Empty, Number(trip.Loyalty.PointsRequired), trip.Loyalty.Entitlement ?? string.Empty,
            trip.Loyalty.RedemptionState, trip.Source,
            Instant(row.UpdatedAt)
        ],
        DriverUtilizationPayload driver =>
        [
            driver.DriverId, driver.DriverName, driver.PropertyId, Number(driver.TripCount), Number(driver.IdleSeconds),
            Number(driver.OvertimeSeconds), driver.RedemptionContributionReference ?? string.Empty, Instant(row.UpdatedAt)
        ],
        VehicleUtilizationPayload vehicle =>
        [
            vehicle.VehicleDescription, Number(vehicle.MileageMeters), Number(vehicle.TripCount), Number(vehicle.DowntimeSeconds),
            vehicle.RedemptionReference ?? string.Empty, Instant(row.UpdatedAt)
        ],
        CancellationPayload cancellation =>
        [
            cancellation.ReasonCode, cancellation.Reason, cancellation.Source, Number(cancellation.NoticeSecondsBeforePickup),
            cancellation.PointsReturnPolicyCode ?? string.Empty, cancellation.PointsReturnReference ?? string.Empty, Instant(row.UpdatedAt)
        ],
        _ => throw new InvalidOperationException("The report row has an unknown payload type.")
    };

    private static string Instant(DateTimeOffset value) => value.UtcDateTime.ToString(InstantFormat, CultureInfo.InvariantCulture);

    private static string Instant(DateTimeOffset? value) => value is null ? string.Empty : Instant(value.Value);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(long? value) => value is null ? string.Empty : Number(value.Value);

    private static string Number(double? value) => value is null ? string.Empty : value.Value.ToString("R", CultureInfo.InvariantCulture);
}

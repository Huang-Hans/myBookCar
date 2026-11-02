using LMS.Reporting.Domain.Rows;
using MongoDB.Bson;

namespace LMS.Reporting.Infrastructure.Documents;

// 按 rowType 把 payload 映射为对应的强类型载荷；每种类型只读取自己的字段，其余字段不会被带出。
internal static class ReportPayloadReader
{
    public static ReportPayload Read(ReportType type, BsonDocument payload)
    {
        var reader = new BsonFieldReader(payload, "payload");

        return type switch
        {
            ReportType.DailyJobSummary => ReadDailyJobSummary(reader),
            ReportType.TripDetail => ReadTripDetail(reader),
            ReportType.DriverUtilization => ReadDriverUtilization(reader),
            ReportType.VehicleUtilization => ReadVehicleUtilization(reader),
            ReportType.Cancellation => ReadCancellation(reader),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type.")
        };
    }

    private static DailyJobSummaryPayload ReadDailyJobSummary(BsonFieldReader reader) => new(
        reader.Text("propertyId"), reader.Int("bookings"), reader.Int("completed"), reader.Int("cancellations"), reader.Int("noShows"),
        reader.OptionalText("redemptionSummary"));

    private static TripDetailPayload ReadTripDetail(BsonFieldReader reader)
    {
        var loyalty = reader.Child("loyalty");

        return new TripDetailPayload(
            reader.Text("tripReference"), reader.Text("propertyId"), reader.Text("guestName"), reader.Instant("scheduledAt"),
            reader.OptionalInstant("actualAt"), reader.OptionalText("driverId"), reader.OptionalText("driverName"),
            reader.OptionalText("vehicleDescription"), reader.OptionalDouble("distanceMeters"), reader.OptionalLong("durationSeconds"),
            new LoyaltyReference(
                loyalty.OptionalText("redemptionId"), loyalty.Long("pointsRequired"), loyalty.OptionalText("entitlement"), loyalty.Text("redemptionState")),
            reader.Text("source"));
    }

    private static DriverUtilizationPayload ReadDriverUtilization(BsonFieldReader reader) => new(
        reader.Text("driverId"), reader.Text("driverName"), reader.Text("propertyId"), reader.Int("tripCount"),
        reader.OptionalLong("idleSeconds"), reader.OptionalLong("overtimeSeconds"), reader.OptionalText("redemptionContributionReference"),
        reader.OptionalFlag("isPartial"));

    private static VehicleUtilizationPayload ReadVehicleUtilization(BsonFieldReader reader) => new(
        reader.Text("vehicleDescription"), reader.OptionalDouble("mileageMeters"), reader.Int("tripCount"),
        reader.OptionalLong("downtimeSeconds"), reader.OptionalText("redemptionReference"), reader.OptionalFlag("isPartial"));

    private static CancellationPayload ReadCancellation(BsonFieldReader reader) => new(
        reader.Text("reasonCode"), reader.Text("reason"), reader.Text("source"), reader.OptionalLong("noticeSecondsBeforePickup"),
        reader.OptionalText("pointsReturnPolicyCode"), reader.OptionalText("pointsReturnReference"));
}

using LMS.Reporting.Domain.Rows;
using MongoDB.Bson;

namespace LMS.Reporting.Infrastructure.Documents;

// ReportPayloadReader 的逆向：按载荷类型写出同名字段。空值写成 BSON null 而不是省略，汇总查询据此区分“未知”与 0。
internal static class ReportPayloadWriter
{
    public static BsonDocument Write(ReportPayload payload) => payload switch
    {
        DailyJobSummaryPayload daily => new BsonDocument
        {
            ["propertyId"] = daily.PropertyId,
            ["bookings"] = daily.Bookings,
            ["completed"] = daily.Completed,
            ["cancellations"] = daily.Cancellations,
            ["noShows"] = daily.NoShows,
            ["redemptionSummary"] = Nullable(daily.RedemptionSummary),
            ["redemption"] = new BsonDocument
            {
                ["reserved"] = daily.Redemption?.Reserved ?? 0,
                ["redeemed"] = daily.Redemption?.Redeemed ?? 0,
                ["returned"] = daily.Redemption?.Returned ?? 0
            }
        },
        TripDetailPayload trip => WriteTripDetail(trip),
        DriverUtilizationPayload driver => WriteDriverUtilization(driver),
        VehicleUtilizationPayload vehicle => WriteVehicleUtilization(vehicle),
        CancellationPayload cancellation => WriteCancellation(cancellation),
        _ => throw new ArgumentOutOfRangeException(nameof(payload), payload.GetType().Name, "Unknown report payload.")
    };

    private static BsonDocument WriteTripDetail(TripDetailPayload trip) => new()
    {
        ["tripReference"] = trip.TripReference,
        ["propertyId"] = trip.PropertyId,
        ["guestName"] = trip.GuestName,
        ["scheduledAt"] = trip.ScheduledAt.UtcDateTime,
        ["actualAt"] = trip.ActualAt is null ? BsonNull.Value : trip.ActualAt.Value.UtcDateTime,
        ["driverId"] = Nullable(trip.DriverId),
        ["driverName"] = Nullable(trip.DriverName),
        ["vehicleDescription"] = Nullable(trip.VehicleDescription),
        ["distanceMeters"] = Nullable(trip.DistanceMeters),
        ["durationSeconds"] = Nullable(trip.DurationSeconds),
        ["loyalty"] = new BsonDocument
        {
            ["redemptionId"] = Nullable(trip.Loyalty.RedemptionId),
            ["pointsRequired"] = trip.Loyalty.PointsRequired,
            ["entitlement"] = Nullable(trip.Loyalty.Entitlement),
            ["redemptionState"] = trip.Loyalty.RedemptionState
        },
        ["source"] = trip.Source
    };

    private static BsonDocument WriteDriverUtilization(DriverUtilizationPayload driver) => new()
    {
        ["driverId"] = driver.DriverId,
        ["driverName"] = driver.DriverName,
        ["propertyId"] = driver.PropertyId,
        ["tripCount"] = driver.TripCount,
        ["idleSeconds"] = Nullable(driver.IdleSeconds),
        ["overtimeSeconds"] = Nullable(driver.OvertimeSeconds),
        ["redemptionContributionReference"] = Nullable(driver.RedemptionContributionReference),
        ["isPartial"] = driver.IsPartial
    };

    private static BsonDocument WriteVehicleUtilization(VehicleUtilizationPayload vehicle) => new()
    {
        ["vehicleDescription"] = vehicle.VehicleDescription,
        ["mileageMeters"] = Nullable(vehicle.MileageMeters),
        ["tripCount"] = vehicle.TripCount,
        ["downtimeSeconds"] = Nullable(vehicle.DowntimeSeconds),
        ["redemptionReference"] = Nullable(vehicle.RedemptionReference),
        ["isPartial"] = vehicle.IsPartial
    };

    private static BsonDocument WriteCancellation(CancellationPayload cancellation) => new()
    {
        ["reasonCode"] = cancellation.ReasonCode,
        ["reason"] = cancellation.Reason,
        ["source"] = cancellation.Source,
        ["noticeSecondsBeforePickup"] = Nullable(cancellation.NoticeSecondsBeforePickup),
        ["pointsReturnPolicyCode"] = Nullable(cancellation.PointsReturnPolicyCode),
        ["pointsReturnReference"] = Nullable(cancellation.PointsReturnReference)
    };

    private static BsonValue Nullable(string? value) => value is null ? BsonNull.Value : value;

    private static BsonValue Nullable(long? value) => value is null ? BsonNull.Value : value.Value;

    private static BsonValue Nullable(double? value) => value is null ? BsonNull.Value : value.Value;
}

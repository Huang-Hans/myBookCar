namespace LMS.Reporting.Domain.Rows;

// ActualAt 为空表示尚未实际发生（Awaiting pickup），不得用演示时间填充。
public sealed record TripDetailPayload(
    string TripReference,
    string PropertyId,
    string GuestName,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? ActualAt,
    string? DriverId,
    string? DriverName,
    string? VehicleDescription,
    double? DistanceMeters,
    long? DurationSeconds,
    LoyaltyReference Loyalty,
    string Source) : ReportPayload;

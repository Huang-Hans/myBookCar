namespace LMS.Dispatch.Domain.Reports;

// 司机登记的事故 / 事件报告。occurredAt 是司机陈述的发生时间（UTC），reportedAt 是服务端登记时间，两者分开记录。
// TripId 可选，且已经由 Booking 核对归属于本司机；原型没有地点与附件，因此不设必填 GPS。
public sealed record DriverIncident(
    string IncidentId,
    string DriverId,
    string PropertyId,
    string VehicleSnapshot,
    IncidentType Type,
    DateTimeOffset OccurredAt,
    string Description,
    string? TripId,
    IncidentStatus Status,
    DateTimeOffset ReportedAt,
    long Version)
{
    public const string IdPrefix = "incident_";
    public const int MaxDescriptionLength = 2000;

    private const long FirstVersion = 1;

    public static DriverIncident Create(
        string driverId,
        string propertyId,
        string vehicleSnapshot,
        IncidentType type,
        DateTimeOffset occurredAt,
        string description,
        string? tripId,
        DateTimeOffset now) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            driverId, propertyId, vehicleSnapshot, type, occurredAt, description, tripId, IncidentStatus.Recorded, now, FirstVersion);
}

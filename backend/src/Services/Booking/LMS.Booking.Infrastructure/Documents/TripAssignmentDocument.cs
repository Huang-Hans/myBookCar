using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class TripAssignmentDocument
{
    [BsonElement("offerId")]
    public string OfferId { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("driverName")]
    public string DriverName { get; init; } = string.Empty;

    [BsonElement("vehicleLabel")]
    public string VehicleLabel { get; init; } = string.Empty;

    [BsonElement("assignedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime AssignedAt { get; init; }

    public static TripAssignmentDocument FromDomain(DriverAssignment assignment) => new()
    {
        OfferId = assignment.OfferId,
        DriverId = assignment.DriverId,
        DriverName = assignment.DriverName,
        VehicleLabel = assignment.VehicleLabel,
        AssignedAt = UtcDateTimes.ToStored(assignment.AssignedAt)
    };

    public DriverAssignment ToDomain() =>
        new(OfferId, DriverId, DriverName, VehicleLabel, UtcDateTimes.FromStored(AssignedAt));
}

using LMS.Booking.Domain.Approvals;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class RouteSnapshotDocument
{
    [BsonElement("pickupName")]
    public string PickupName { get; init; } = string.Empty;

    [BsonElement("dropoffName")]
    public string DropoffName { get; init; } = string.Empty;

    public static RouteSnapshotDocument FromDomain(RouteSnapshot route) => new() { PickupName = route.PickupName, DropoffName = route.DropoffName };

    public RouteSnapshot ToDomain() => new(PickupName, DropoffName);
}

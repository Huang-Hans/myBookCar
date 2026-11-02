using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class DriverOfferDocument
{
    [BsonId]
    public string OfferId { get; init; } = string.Empty;

    [BsonElement("tripId")]
    public string TripId { get; init; } = string.Empty;

    [BsonElement("tripReference")]
    public string TripReference { get; init; } = string.Empty;

    [BsonElement("driverId")]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("trip")]
    public OfferTripSummaryDocument Trip { get; init; } = new();

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; init; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    [BsonElement("respondedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? RespondedAt { get; init; }

    // 以下为客服派单新增字段；存量邀请没有这些元素，读取时轮次为 0、快照为空，也不参与占用约束。
    [BsonElement("round")]
    public int Round { get; init; }

    [BsonElement("driverName")]
    public string? DriverName { get; init; }

    [BsonElement("createdBy")]
    public string? CreatedBy { get; init; }

    // 仅在邀请占用中（待响应或处理中）时等于行程 / 司机标识，其余为 null；唯一部分索引借此保证同一行程、同一司机各自至多一张占用中的邀请。
    [BsonElement("openTripKey")]
    public string? OpenTripKey { get; init; }

    [BsonElement("openDriverKey")]
    public string? OpenDriverKey { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static DriverOfferDocument FromDomain(DriverOffer offer) => new()
    {
        OfferId = offer.OfferId,
        TripId = offer.TripId,
        TripReference = offer.TripReference,
        DriverId = offer.DriverId,
        PropertyId = offer.PropertyId,
        Trip = OfferTripSummaryDocument.FromDomain(offer.Trip),
        Status = WireEnum.ToWire(offer.Status),
        ExpiresAt = UtcDateTimes.ToStored(offer.ExpiresAt),
        CreatedAt = UtcDateTimes.ToStored(offer.CreatedAt),
        RespondedAt = offer.RespondedAt is null ? null : UtcDateTimes.ToStored(offer.RespondedAt.Value),
        Round = offer.Round,
        DriverName = offer.DriverName,
        CreatedBy = offer.CreatedBy,
        OpenTripKey = offer.IsOpen ? offer.TripId : null,
        OpenDriverKey = offer.IsOpen ? offer.DriverId : null,
        Version = offer.Version
    };

    public DriverOffer ToDomain() => new(
        OfferId,
        TripId,
        TripReference,
        DriverId,
        PropertyId,
        Trip.ToDomain(),
        StoredValues.ParseEnum<DriverOfferStatus>(Status, "status"),
        UtcDateTimes.FromStored(ExpiresAt),
        UtcDateTimes.FromStored(CreatedAt),
        RespondedAt is null ? null : UtcDateTimes.FromStored(RespondedAt.Value),
        Version,
        Round,
        DriverName,
        CreatedBy);
}

using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

[BsonIgnoreExtraElements]
internal sealed class TripDocument
{
    [BsonId]
    public string TripId { get; init; } = string.Empty;

    [BsonElement("tripReference")]
    public string TripReference { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("guest")]
    public TripGuestDocument Guest { get; init; } = new();

    [BsonElement("route")]
    public TripRouteDocument Route { get; init; } = new();

    [BsonElement("pickupAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime PickupAt { get; init; }

    [BsonElement("pickupMode")]
    public string PickupMode { get; init; } = string.Empty;

    [BsonElement("serviceType")]
    public string ServiceType { get; init; } = string.Empty;

    [BsonElement("vehicleClass")]
    public string VehicleClass { get; init; } = string.Empty;

    [BsonElement("passengers")]
    public int Passengers { get; init; }

    [BsonElement("luggage")]
    public int Luggage { get; init; }

    [BsonElement("specialRequests")]
    public string? SpecialRequests { get; init; }

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("points")]
    public TripPointsDocument Points { get; init; } = new();

    [BsonElement("cancellation")]
    public TripCancellationDocument? Cancellation { get; init; }

    [BsonElement("bookedBy")]
    public BookedByDocument BookedBy { get; init; } = new();

    [BsonElement("confirmationLanguage")]
    public string ConfirmationLanguage { get; init; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    // 以下为客户自助与司机履约新增字段；存量文档没有这些元素，读取时取默认值（渠道为客服代订、无指派、无履约时间）。
    [BsonElement("channel")]
    public string? Channel { get; init; }

    [BsonElement("product")]
    public string? Product { get; init; }

    [BsonElement("assignment")]
    public TripAssignmentDocument? Assignment { get; init; }

    [BsonElement("fulfillment")]
    public TripFulfillmentDocument? Fulfillment { get; init; }

    [BsonElement("events")]
    public List<TripEventDocument>? Events { get; init; }

    // 仅在行程有指派且未终态时为司机标识，其余为 null；唯一部分索引借此保证同一司机同时只有一个执行中的行程。
    [BsonElement("activeDriverId")]
    public string? ActiveDriverId { get; init; }

    [BsonElement("closedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ClosedAt { get; init; }

    // 修改记录；存量行程没有该元素，读取时为空。
    [BsonElement("modifications")]
    public List<TripModificationDocument>? Modifications { get; init; }

    // 優惠券訂單才有。存量行程沒有這兩個元素。
    [BsonElement("couponId")]
    [BsonIgnoreIfNull]
    public string? CouponId { get; init; }

    [BsonElement("couponRedemptionKey")]
    [BsonIgnoreIfNull]
    public string? CouponRedemptionKey { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static TripDocument FromDomain(Trip trip) => new()
    {
        TripId = trip.TripId,
        TripReference = trip.TripReference,
        PropertyId = trip.PropertyId,
        Guest = new TripGuestDocument
        {
            GuestId = trip.Guest.GuestId,
            Name = trip.Guest.Name,
            TierCode = trip.Guest.TierCode,
            RoomNumber = trip.Guest.RoomNumber,
            MemberId = trip.Guest.MemberId,
            PhoneMasked = trip.Guest.PhoneMasked
        },
        Route = new TripRouteDocument
        {
            PickupLocationId = trip.Route.PickupLocationId,
            PickupName = trip.Route.PickupName,
            DropoffLocationId = trip.Route.DropoffLocationId,
            DropoffName = trip.Route.DropoffName,
            IsCrossBorder = trip.Route.IsCrossBorder
        },
        PickupAt = UtcDateTimes.ToStored(trip.PickupAt),
        PickupMode = WireEnum.ToWire(trip.PickupMode),
        ServiceType = WireEnum.ToWire(trip.ServiceType),
        VehicleClass = WireEnum.ToWire(trip.VehicleClass),
        Passengers = trip.Passengers,
        Luggage = trip.Luggage,
        SpecialRequests = trip.SpecialRequests,
        Status = WireEnum.ToWire(trip.Status),
        Points = new TripPointsDocument
        {
            Required = trip.Points.Required,
            Reference = trip.Points.Reference,
            State = WireEnum.ToWire(trip.Points.State)
        },
        Cancellation = trip.Cancellation is null ? null : new TripCancellationDocument
        {
            ReasonCode = trip.Cancellation.ReasonCode,
            Comments = trip.Cancellation.Comments,
            CancelledBy = trip.Cancellation.CancelledBy,
            CancelledAt = UtcDateTimes.ToStored(trip.Cancellation.CancelledAt),
            PolicyReference = trip.Cancellation.PolicyReference
        },
        BookedBy = new BookedByDocument
        {
            UserId = trip.BookedBy.UserId,
            RoleCode = trip.BookedBy.RoleCode,
            Name = trip.BookedBy.Name
        },
        ConfirmationLanguage = trip.ConfirmationLanguage,
        CreatedAt = UtcDateTimes.ToStored(trip.CreatedAt),
        Channel = WireEnum.ToWire(trip.Channel),
        Product = trip.Product is null ? null : WireEnum.ToWire(trip.Product.Value),
        Assignment = trip.Assignment is null ? null : TripAssignmentDocument.FromDomain(trip.Assignment),
        Fulfillment = TripFulfillmentDocument.FromDomain(trip.Fulfillment),
        Events = trip.Events.Select(TripEventDocument.FromDomain).ToList(),
        ActiveDriverId = trip.Assignment is not null && !trip.IsTerminal ? trip.Assignment.DriverId : null,
        ClosedAt = trip.ClosedAt is null ? null : UtcDateTimes.ToStored(trip.ClosedAt.Value),
        Modifications = trip.Modifications?.Select(TripModificationDocument.FromDomain).ToList(),
        CouponId = trip.CouponId,
        CouponRedemptionKey = trip.CouponRedemptionKey,
        Version = trip.Version
    };

    public Trip ToDomain() => new(
        TripId,
        TripReference,
        PropertyId,
        new TripGuest(Guest.GuestId, Guest.Name, Guest.TierCode, Guest.RoomNumber, Guest.MemberId, Guest.PhoneMasked),
        new TripRoute(Route.PickupLocationId, Route.PickupName, Route.DropoffLocationId, Route.DropoffName, Route.IsCrossBorder),
        UtcDateTimes.FromStored(PickupAt),
        StoredValues.ParseEnum<PickupMode>(PickupMode, "pickupMode"),
        StoredValues.ParseEnum<ServiceType>(ServiceType, "serviceType"),
        StoredValues.ParseEnum<VehicleClass>(VehicleClass, "vehicleClass"),
        Passengers,
        Luggage,
        SpecialRequests,
        StoredValues.ParseEnum<TripStatus>(Status, "status"),
        new TripPoints(Points.Required, Points.Reference, StoredValues.ParseEnum<PointsState>(Points.State, "points.state")),
        Cancellation is null ? null : new TripCancellation(
            Cancellation.ReasonCode,
            Cancellation.Comments,
            Cancellation.CancelledBy,
            UtcDateTimes.FromStored(Cancellation.CancelledAt),
            Cancellation.PolicyReference),
        new BookedBy(BookedBy.UserId, BookedBy.RoleCode, BookedBy.Name),
        ConfirmationLanguage,
        UtcDateTimes.FromStored(CreatedAt),
        string.IsNullOrEmpty(Channel) ? TripChannel.Console : StoredValues.ParseEnum<TripChannel>(Channel, "channel"),
        string.IsNullOrEmpty(Product) ? null : StoredValues.ParseEnum<ServiceProduct>(Product, "product"),
        Assignment?.ToDomain(),
        Fulfillment?.ToDomain() ?? TripFulfillment.Empty,
        Events?.Select(item => item.ToDomain()).ToList() ?? [],
        ClosedAt is null ? null : UtcDateTimes.FromStored(ClosedAt.Value),
        Version,
        Modifications?.Select(item => item.ToDomain()).ToList(),
        CouponId,
        CouponRedemptionKey);
}

internal sealed class TripGuestDocument
{
    [BsonElement("guestId")]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("tierCode")]
    public string TierCode { get; init; } = string.Empty;

    [BsonElement("roomNumber")]
    public string? RoomNumber { get; init; }

    [BsonElement("memberId")]
    public string? MemberId { get; init; }

    [BsonElement("phoneMasked")]
    public string? PhoneMasked { get; init; }
}

internal sealed class TripRouteDocument
{
    [BsonElement("pickupLocationId")]
    public string PickupLocationId { get; init; } = string.Empty;

    [BsonElement("pickupName")]
    public string PickupName { get; init; } = string.Empty;

    [BsonElement("dropoffLocationId")]
    public string DropoffLocationId { get; init; } = string.Empty;

    [BsonElement("dropoffName")]
    public string DropoffName { get; init; } = string.Empty;

    [BsonElement("isCrossBorder")]
    public bool IsCrossBorder { get; init; }
}

internal sealed class TripPointsDocument
{
    [BsonElement("required")]
    public long Required { get; init; }

    [BsonElement("reference")]
    public string? Reference { get; init; }

    [BsonElement("state")]
    public string State { get; init; } = string.Empty;
}

internal sealed class TripCancellationDocument
{
    [BsonElement("reasonCode")]
    public string ReasonCode { get; init; } = string.Empty;

    [BsonElement("comments")]
    public string Comments { get; init; } = string.Empty;

    [BsonElement("cancelledBy")]
    public string CancelledBy { get; init; } = string.Empty;

    [BsonElement("cancelledAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CancelledAt { get; init; }

    [BsonElement("policyReference")]
    public string? PolicyReference { get; init; }
}

[BsonIgnoreExtraElements]
internal sealed class BookedByDocument
{
    [BsonElement("userId")]
    public string UserId { get; init; } = string.Empty;

    [BsonElement("roleCode")]
    public string RoleCode { get; init; } = string.Empty;

    [BsonElement("name")]
    public string? Name { get; init; }
}

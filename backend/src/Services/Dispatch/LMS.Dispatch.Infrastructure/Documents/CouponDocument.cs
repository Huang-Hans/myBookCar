using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Coupons;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class CouponDocument
{
    [BsonId]
    public string CouponId { get; init; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("memberId")]
    public string MemberId { get; init; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; init; } = string.Empty;

    [BsonElement("faceAmount")]
    public int FaceAmount { get; init; }

    [BsonElement("scope")]
    public string Scope { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    [BsonIgnoreIfNull]
    public string? PropertyId { get; init; }

    [BsonElement("vehicleRestriction")]
    [BsonIgnoreIfNull]
    public string? VehicleRestriction { get; init; }

    [BsonElement("validFrom")]
    public string ValidFrom { get; init; } = string.Empty;

    [BsonElement("validTo")]
    public string ValidTo { get; init; } = string.Empty;

    [BsonElement("issuedQuantity")]
    public int IssuedQuantity { get; init; }

    [BsonElement("redeemedCount")]
    public int RedeemedCount { get; init; }

    [BsonElement("disabled")]
    public bool Disabled { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static CouponDocument FromDomain(Coupon coupon) => new()
    {
        CouponId = coupon.CouponId,
        Code = coupon.Code,
        Name = coupon.Name,
        MemberId = coupon.MemberId,
        Type = WireEnum.ToWire(coupon.Type),
        FaceAmount = coupon.FaceAmount,
        Scope = WireEnum.ToWire(coupon.Scope),
        PropertyId = coupon.PropertyId,
        VehicleRestriction = coupon.VehicleRestriction is null ? null : WireEnum.ToWire(coupon.VehicleRestriction.Value),
        ValidFrom = StoredValues.FromDate(coupon.ValidFrom),
        ValidTo = StoredValues.FromDate(coupon.ValidTo),
        IssuedQuantity = coupon.IssuedQuantity,
        RedeemedCount = coupon.RedeemedCount,
        Disabled = coupon.Disabled,
        Version = coupon.Version
    };

    public Coupon ToDomain() => new(
        CouponId,
        Code,
        Name,
        MemberId,
        StoredValues.ParseEnum<CouponType>(Type, "type"),
        FaceAmount,
        StoredValues.ParseEnum<CouponScope>(Scope, "scope"),
        PropertyId,
        string.IsNullOrEmpty(VehicleRestriction) ? null : StoredValues.ParseEnum<CouponVehicleRestriction>(VehicleRestriction, "vehicleRestriction"),
        StoredValues.ToDate(ValidFrom, "validFrom"),
        StoredValues.ToDate(ValidTo, "validTo"),
        IssuedQuantity,
        RedeemedCount,
        Disabled,
        Version);
}

using LMS.Dispatch.Domain.Coupons;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class CouponRedemptionDocument
{
    [BsonId]
    public string RedemptionKey { get; init; } = string.Empty;

    [BsonElement("couponId")]
    public string CouponId { get; init; } = string.Empty;

    [BsonElement("restored")]
    public bool Restored { get; init; }

    public static CouponRedemptionDocument FromDomain(CouponRedemption redemption) => new()
    {
        RedemptionKey = redemption.RedemptionKey,
        CouponId = redemption.CouponId,
        Restored = redemption.Restored
    };

    public CouponRedemption ToDomain() => new(RedemptionKey, CouponId, Restored);
}

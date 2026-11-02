namespace LMS.Dispatch.Domain.Coupons;

// 一張優惠券綁定一名會員。券碼全庫不區分大小寫唯一。已使用次數從 0 起算。
public sealed record Coupon(
    string CouponId,
    string Code,
    string Name,
    string MemberId,
    CouponType Type,
    int FaceAmount,
    CouponScope Scope,
    string? PropertyId,
    CouponVehicleRestriction? VehicleRestriction,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int IssuedQuantity,
    int RedeemedCount,
    bool Disabled,
    long Version)
{
    private const string IdPrefix = "coupon_";
    private const long FirstVersion = 1;

    public static Coupon Create(
        string code,
        string name,
        string memberId,
        CouponType type,
        int faceAmount,
        CouponScope scope,
        string? propertyId,
        CouponVehicleRestriction? vehicleRestriction,
        DateOnly validFrom,
        DateOnly validTo,
        int issuedQuantity,
        bool disabled) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            code,
            name,
            memberId,
            type,
            faceAmount,
            scope,
            propertyId,
            vehicleRestriction,
            validFrom,
            validTo,
            issuedQuantity,
            0,
            disabled,
            FirstVersion);

    // 改成所有物業或限定車型時，清掉不再適用的物業或車型，除非這次請求明確帶了新值。
    public Coupon Apply(CouponChanges changes)
    {
        var scope = changes.Scope.ValueOr(Scope);
        var propertyId = changes.PropertyId.ValueOrCurrent(PropertyId);
        var vehicleRestriction = changes.VehicleRestriction.ValueOrCurrent(VehicleRestriction);
        if (changes.Scope.IsSet)
        {
            if (scope != CouponScope.Property && !changes.PropertyId.IsSet)
            {
                propertyId = null;
            }

            if (scope != CouponScope.Vehicle && !changes.VehicleRestriction.IsSet)
            {
                vehicleRestriction = null;
            }
        }

        return this with
        {
            Code = changes.Code.ValueOr(Code),
            Name = changes.Name.ValueOr(Name),
            MemberId = changes.MemberId.ValueOr(MemberId),
            Type = changes.Type.ValueOr(Type),
            FaceAmount = changes.FaceAmount.ValueOr(FaceAmount),
            Scope = scope,
            PropertyId = string.IsNullOrWhiteSpace(propertyId) ? null : propertyId.Trim(),
            VehicleRestriction = vehicleRestriction,
            ValidFrom = changes.ValidFrom.ValueOr(ValidFrom),
            ValidTo = changes.ValidTo.ValueOr(ValidTo),
            IssuedQuantity = changes.IssuedQuantity.ValueOr(IssuedQuantity),
            Disabled = changes.Disabled.ValueOr(Disabled)
        };
    }

    // 一張優惠券訂單使用一次。呼叫方須先確認尚未用完。
    public Coupon RedeemOne() => this with { RedeemedCount = RedeemedCount + 1 };

    // 訂單取消後退還一次。呼叫方須先確認已使用次數大於 0。
    public Coupon RestoreOne() => this with { RedeemedCount = RedeemedCount - 1 };

    public CouponStatus StatusOn(DateOnly today)
    {
        if (Disabled)
        {
            return CouponStatus.Disabled;
        }

        if (RedeemedCount >= IssuedQuantity)
        {
            return CouponStatus.Exhausted;
        }

        if (today < ValidFrom)
        {
            return CouponStatus.Upcoming;
        }

        return today > ValidTo ? CouponStatus.Expired : CouponStatus.Active;
    }
}

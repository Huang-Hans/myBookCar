using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Domain.Coupons;

// 優惠券的局部更新。已使用次數不在此列，避免把兌付紀錄改掉。
public sealed record CouponChanges(
    Optional<string> Code,
    Optional<string> Name,
    Optional<string> MemberId,
    Optional<CouponType> Type,
    Optional<int> FaceAmount,
    Optional<CouponScope> Scope,
    Optional<string> PropertyId,
    Optional<CouponVehicleRestriction?> VehicleRestriction,
    Optional<DateOnly> ValidFrom,
    Optional<DateOnly> ValidTo,
    Optional<int> IssuedQuantity,
    Optional<bool> Disabled)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(
            ("code", Code.IsSet),
            ("name", Name.IsSet),
            ("memberId", MemberId.IsSet),
            ("type", Type.IsSet),
            ("faceAmount", FaceAmount.IsSet),
            ("scope", Scope.IsSet),
            ("propertyId", PropertyId.IsSet),
            ("vehicleRestriction", VehicleRestriction.IsSet),
            ("validFrom", ValidFrom.IsSet),
            ("validTo", ValidTo.IsSet),
            ("issuedQuantity", IssuedQuantity.IsSet),
            ("disabled", Disabled.IsSet));
}

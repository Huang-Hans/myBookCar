using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Dispatch.Domain.Coupons;

public static class CouponRules
{
    public static IReadOnlyList<FieldIssue> Validate(Coupon coupon)
    {
        var issues = new List<FieldIssue>();
        AddFaceAmount(issues, coupon);
        AddScope(issues, coupon);
        if (coupon.ValidTo < coupon.ValidFrom)
        {
            issues.Add(new FieldIssue("validTo", "Must be on or after validFrom"));
        }

        if (coupon.IssuedQuantity < 1)
        {
            issues.Add(new FieldIssue("issuedQuantity", "Must be a positive integer"));
        }

        if (coupon.RedeemedCount < 0 || coupon.RedeemedCount > coupon.IssuedQuantity)
        {
            issues.Add(new FieldIssue("redeemedCount", "Must be between zero and issuedQuantity"));
        }

        return issues;
    }

    private static void AddFaceAmount(List<FieldIssue> issues, Coupon coupon)
    {
        var valid = coupon.Type switch
        {
            CouponType.FreeTrip => coupon.FaceAmount >= 1,
            CouponType.Discount => coupon.FaceAmount is >= 1 and <= 100,
            CouponType.FareDeduction => coupon.FaceAmount >= 1,
            _ => false
        };
        if (!valid)
        {
            issues.Add(new FieldIssue("faceAmount", FaceAmountMessage(coupon.Type)));
        }
    }

    private static void AddScope(List<FieldIssue> issues, Coupon coupon)
    {
        switch (coupon.Scope)
        {
            case CouponScope.Property when string.IsNullOrWhiteSpace(coupon.PropertyId) || coupon.VehicleRestriction is not null:
                issues.Add(new FieldIssue("propertyId", "Is required when scope is PROPERTY"));
                break;
            case CouponScope.Vehicle when coupon.VehicleRestriction is null || coupon.PropertyId is not null:
                issues.Add(new FieldIssue("vehicleRestriction", "Is required when scope is VEHICLE"));
                break;
            case CouponScope.AllProperties when coupon.PropertyId is not null || coupon.VehicleRestriction is not null:
                issues.Add(new FieldIssue("scope", "PROPERTY and vehicle fields must be omitted when scope is ALL_PROPERTIES"));
                break;
        }
    }

    private static string FaceAmountMessage(CouponType type) => type switch
    {
        CouponType.Discount => "Must be a percentage from 1 to 100",
        CouponType.FareDeduction => "Must be a positive amount in MOP",
        _ => "Must be a positive number of trips"
    };
}

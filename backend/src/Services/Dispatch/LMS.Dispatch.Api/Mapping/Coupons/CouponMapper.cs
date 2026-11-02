using System.Globalization;
using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Coupons;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Api.Mapping.Coupons;

internal static class CouponMapper
{
    public static ListCouponsQuery ToListQuery(CouponListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, default, request.MemberId, request.Type, request.Status);

    public static RequestParseResult<CreateCouponCommand> ToCreateCommand(
        CreateCouponRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var typeParsed = WireEnum.TryParse<CouponType>(request.Type, out var type);
        var scopeParsed = WireEnum.TryParse<CouponScope>(request.Scope, out var scope);
        if (!typeParsed)
        {
            errors.RequiredEnum<CouponType>("type", request.Type);
        }

        if (!scopeParsed)
        {
            errors.RequiredEnum<CouponScope>("scope", request.Scope);
        }

        var validFrom = errors.RequiredDate("validFrom", request.ValidFrom);
        var validTo = errors.RequiredDate("validTo", request.ValidTo);
        if (HasDate(request.ValidFrom) && HasDate(request.ValidTo) && validTo < validFrom)
        {
            errors.Add("validTo", "Must be on or after validFrom");
        }

        var command = new CreateCouponCommand(
            errors.RequiredText("code", request.Code),
            errors.RequiredText("name", request.Name),
            errors.RequiredText("memberId", request.MemberId),
            type,
            ReadFaceAmount(errors, typeParsed, type, request.FaceAmount),
            scope,
            ReadPropertyId(errors, scopeParsed, scope, request.PropertyId),
            ReadVehicleRestriction(errors, scopeParsed, scope, request.VehicleRestriction),
            validFrom,
            validTo,
            ReadIssuedQuantity(errors, request.IssuedQuantity),
            request.Disabled ?? false,
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateCouponCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateCouponCommand>.Success(command);
    }

    public static RequestParseResult<UpdateCouponCommand> ToUpdateCommand(
        string couponId,
        long expectedVersion,
        PatchCouponRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = new CouponChanges(
            errors.PatchText("code", request.Code),
            errors.PatchText("name", request.Name),
            errors.PatchText("memberId", request.MemberId),
            errors.PatchEnum<CouponType>("type", request.Type),
            ReadFaceAmount(errors, request.FaceAmount),
            errors.PatchEnum<CouponScope>("scope", request.Scope),
            request.PropertyId.PatchNullableText(),
            ReadVehicleRestriction(errors, request.VehicleRestriction),
            errors.PatchDate("validFrom", request.ValidFrom),
            errors.PatchDate("validTo", request.ValidTo),
            ReadIssuedQuantity(errors, request.IssuedQuantity),
            errors.PatchBool("disabled", request.Disabled));
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateCouponCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateCouponCommand>.Success(new UpdateCouponCommand(couponId, expectedVersion, changes, context));
    }

    public static CouponView ToView(CouponCard card)
    {
        var coupon = card.Coupon;

        return new CouponView(
            coupon.CouponId,
            coupon.Code,
            coupon.Name,
            coupon.MemberId,
            WireEnum.ToWire(coupon.Type),
            coupon.FaceAmount,
            WireEnum.ToWire(coupon.Scope),
            coupon.PropertyId,
            coupon.VehicleRestriction is null ? null : WireEnum.ToWire(coupon.VehicleRestriction.Value),
            coupon.ValidFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            coupon.ValidTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            coupon.IssuedQuantity,
            coupon.RedeemedCount,
            WireEnum.ToWire(coupon.StatusOn(card.Today)),
            coupon.Version);
    }

    public static CouponSummaryView ToSummary(CouponSummaryCounts counts) =>
        new(counts.Active, counts.IssuedQuantity, counts.RedeemedCount, counts.Upcoming);

    private static int ReadFaceAmount(FieldErrorCollector errors, bool typeParsed, CouponType type, int? amount)
    {
        var value = errors.RequiredInt("faceAmount", amount);
        if (!typeParsed || amount is null)
        {
            return value;
        }

        var valid = type switch
        {
            CouponType.Discount => amount is >= 1 and <= 100,
            _ => amount >= 1
        };
        if (!valid)
        {
            errors.Add("faceAmount", type == CouponType.Discount ? "Must be a percentage from 1 to 100" : "Must be a positive integer");
        }

        return value;
    }

    private static int ReadIssuedQuantity(FieldErrorCollector errors, int? quantity)
    {
        var value = errors.RequiredInt("issuedQuantity", quantity);
        if (quantity is < 1)
        {
            errors.Add("issuedQuantity", "Must be a positive integer");
        }

        return value;
    }

    private static string? ReadPropertyId(FieldErrorCollector errors, bool scopeParsed, CouponScope scope, string? propertyId)
    {
        if (!scopeParsed)
        {
            return null;
        }

        if (scope == CouponScope.Property)
        {
            return errors.RequiredText("propertyId", propertyId);
        }

        if (!string.IsNullOrWhiteSpace(propertyId))
        {
            errors.Add("propertyId", "Must be omitted unless scope is PROPERTY");
        }

        return null;
    }

    private static CouponVehicleRestriction? ReadVehicleRestriction(
        FieldErrorCollector errors,
        bool scopeParsed,
        CouponScope scope,
        string? vehicleRestriction)
    {
        if (!scopeParsed)
        {
            return null;
        }

        if (scope == CouponScope.Vehicle)
        {
            return errors.RequiredEnum<CouponVehicleRestriction>("vehicleRestriction", vehicleRestriction);
        }

        if (!string.IsNullOrWhiteSpace(vehicleRestriction))
        {
            errors.Add("vehicleRestriction", "Must be omitted unless scope is VEHICLE");
        }

        return null;
    }

    private static Optional<int> ReadFaceAmount(FieldErrorCollector errors, Optional<int?> amount)
    {
        var value = errors.PatchInt("faceAmount", amount);
        if (value.IsSet && value.Value < 1)
        {
            errors.Add("faceAmount", "Must be a positive integer");
        }

        return value;
    }

    private static Optional<int> ReadIssuedQuantity(FieldErrorCollector errors, Optional<int?> quantity)
    {
        var value = errors.PatchInt("issuedQuantity", quantity);
        if (value.IsSet && value.Value < 1)
        {
            errors.Add("issuedQuantity", "Must be a positive integer");
        }

        return value;
    }

    private static Optional<CouponVehicleRestriction?> ReadVehicleRestriction(FieldErrorCollector errors, Optional<string?> value)
    {
        if (!value.IsSet)
        {
            return default;
        }

        if (string.IsNullOrWhiteSpace(value.Value))
        {
            return Optional<CouponVehicleRestriction?>.Of(null);
        }

        return WireEnum.TryParse<CouponVehicleRestriction>(value.Value, out var parsed)
            ? Optional<CouponVehicleRestriction?>.Of(parsed)
            : AddVehicleRestrictionError(errors);
    }

    private static Optional<CouponVehicleRestriction?> AddVehicleRestrictionError(FieldErrorCollector errors)
    {
        errors.Add("vehicleRestriction", $"Must be one of: {string.Join(", ", WireEnum.WireNames<CouponVehicleRestriction>())}");

        return default;
    }

    private static bool HasDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}

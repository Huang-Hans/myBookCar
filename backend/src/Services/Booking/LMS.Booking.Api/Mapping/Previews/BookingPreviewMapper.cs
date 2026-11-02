using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Api.Mapping.Products;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Previews;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Previews.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.Previews;

internal static class BookingPreviewMapper
{
    private const string PointsBasis = "REFERENCE_ESTIMATE";

    public static BookingPreviewView ToView(BookingPreview preview) => new(
        preview.CanSubmit,
        preview.WillWaitlist,
        preview.RequiresApproval,
        preview.BlockedReasons.Select(reason => new BookingPreviewReasonView(reason.Code, reason.Field, reason.Message)).ToList(),
        new BookingPreviewEligibilityView(
            preview.Eligibility.Tier,
            preview.Eligibility.TierMinimum,
            preview.Eligibility.PlayVerified,
            preview.Eligibility.PlayWindowDays,
            preview.Eligibility.EntitledVehicleClasses,
            preview.Eligibility.IsVehicleEntitled),
        new BookingPreviewPointsView(
            preview.Points.Required, preview.Points.Available, preview.Points.Shortfall, preview.Points.EstimatedBalanceAfter, PointsBasis),
        preview.EvaluatedAt);

    public static RequestParseResult<BookingPreviewQuery> ToQuery(BookingPreviewRequest request, CommandContext context, SelfServiceOptions? products = null)
    {
        var errors = new FieldErrorCollector();
        var pickupMode = errors.RequiredEnum<PickupMode>("pickupMode", request.PickupMode);
        if (pickupMode != PickupMode.Asap && request.PickupAt is null)
        {
            errors.Add("pickupAt", "Required unless pickupMode is ASAP");
        }

        var query = new BookingPreviewQuery(
            errors.RequiredText("guestId", request.GuestId),
            errors.RequiredText("propertyId", request.PropertyId),
            errors.RequiredText("pickupLocationId", request.PickupLocationId),
            errors.RequiredText("dropoffLocationId", request.DropoffLocationId),
            request.PickupAt,
            pickupMode,
            errors.RequiredEnum<ServiceType>("serviceType", request.ServiceType),
            ProductVehicleMapper.Resolve(errors, request.ProductCode, request.VehicleClass, products ?? new SelfServiceOptions()),
            errors.RequiredInt("passengers", request.Passengers),
            errors.RequiredInt("luggage", request.Luggage),
            context,
            request.ProductCode is null ? null : errors.RequiredEnum<ServiceProduct>("productCode", request.ProductCode));

        return errors.HasErrors
            ? RequestParseResult<BookingPreviewQuery>.Fail(errors.ToFailure())
            : RequestParseResult<BookingPreviewQuery>.Success(query);
    }
}

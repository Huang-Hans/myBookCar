using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Api.Mapping.Products;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Customer;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.SelfService.Commands;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.Customer;

internal static class CustomerRequestMapper
{
    private const int MaxTextLength = 500;

    public static RequestParseResult<CreateSelfServiceTripCommand> ToCreateCommand(
        string guestId,
        CreateCustomerTripRequest request,
        string idempotencyKey,
        CommandContext context, SelfServiceOptions? products = null)
    {
        var errors = new FieldErrorCollector();
        var pickupMode = errors.RequiredEnum<PickupMode>("pickupMode", request.PickupMode);
        if (pickupMode == PickupMode.Today)
        {
            errors.Add("pickupMode", "Must be ASAP or SCHEDULED");
        }

        if (pickupMode != PickupMode.Asap && request.PickupAt is null)
        {
            errors.Add("pickupAt", "Required unless pickupMode is ASAP");
        }

        var booking = new SelfBookingRequest(
            guestId,
            errors.RequiredText("productCode", request.ProductCode),
            errors.RequiredText("pickupLocationId", request.PickupLocationId),
            errors.RequiredText("dropoffLocationId", request.DropoffLocationId),
            pickupMode,
            request.PickupAt,
            ProductVehicleMapper.Resolve(errors, request.ProductCode, request.VehicleClass, products ?? new SelfServiceOptions()),
            errors.RequiredInt("passengers", request.Passengers),
            request.Luggage ?? 0);
        var command = new CreateSelfServiceTripCommand(
            booking,
            OptionalText(errors, "specialRequests", request.SpecialRequests),
            OptionalText(errors, "confirmationLanguage", request.ConfirmationLanguage),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateSelfServiceTripCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateSelfServiceTripCommand>.Success(command);
    }

    public static RequestParseResult<CancelSelfServiceTripCommand> ToCancelCommand(
        string tripId,
        string guestId,
        CancelCustomerTripRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var comments = OptionalText(errors, "comments", request.Comments);

        return errors.HasErrors
            ? RequestParseResult<CancelSelfServiceTripCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CancelSelfServiceTripCommand>.Success(
                new CancelSelfServiceTripCommand(tripId, guestId, comments, idempotencyKey, context));
    }

    // 可选文本：提供时不得为空白，且不超过 500 字符。
    private static string? OptionalText(FieldErrorCollector errors, string field, string? value)
    {
        if (value is null)
        {
            return null;
        }

        var text = errors.RequiredText(field, value);
        if (text.Length > MaxTextLength)
        {
            errors.Add(field, $"Must not exceed {MaxTextLength} characters");
        }

        return text;
    }
}

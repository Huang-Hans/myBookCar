using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Api.Mapping.Products;
using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Trips;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.Trips;

internal static class TripMapper
{
    public static TripView ToView(Trip trip) => new(
        trip.TripId,
        trip.TripReference,
        trip.PropertyId,
        new TripGuestView(trip.Guest.GuestId, trip.Guest.Name, trip.Guest.TierCode, trip.Guest.RoomNumber, trip.Guest.MemberId, trip.Guest.PhoneMasked),
        new TripRouteView(trip.Route.PickupLocationId, trip.Route.PickupName, trip.Route.DropoffLocationId, trip.Route.DropoffName, trip.Route.IsCrossBorder),
        trip.PickupAt,
        WireEnum.ToWire(trip.PickupMode),
        WireEnum.ToWire(trip.ServiceType),
        WireEnum.ToWire(trip.VehicleClass),
        trip.Passengers,
        trip.Luggage,
        trip.SpecialRequests,
        WireEnum.ToWire(trip.Status),
        WireEnum.ToWire(TripStages.Of(trip)),
        new TripPointsView(trip.Points.Required, trip.Points.Reference, WireEnum.ToWire(trip.Points.State)),
        trip.Cancellation is null ? null : new TripCancellationView(
            trip.Cancellation.ReasonCode,
            trip.Cancellation.Comments,
            trip.Cancellation.CancelledBy,
            trip.Cancellation.CancelledAt,
            trip.Cancellation.PolicyReference),
        new BookedByView(trip.BookedBy.UserId, trip.BookedBy.RoleCode, trip.BookedBy.Name),
        trip.ConfirmationLanguage,
        trip.Version,
        TripTimelineMapper.ToView(trip.LastModification),
        trip.Assignment is null
            ? null
            : new TripAssignmentView(trip.Assignment.DriverId, trip.Assignment.DriverName, trip.Assignment.VehicleLabel, trip.Assignment.AssignedAt),
        WireEnum.ToWire(trip.Channel),
        trip.Product is { } product ? WireEnum.ToWire(product) : null);

    public static TripCancellationPolicyView ToView(TripCancellationPolicy policy) => new(
        policy.TripReference, policy.PropertyId, WireEnum.ToWire(policy.Status), policy.PolicyReference);

    public static RequestParseResult<CreateTripCommand> ToCreateCommand(
        CreateTripRequest request,
        string idempotencyKey,
        CommandContext context, SelfServiceOptions? products = null)
    {
        var errors = new FieldErrorCollector();
        var pickupMode = errors.RequiredEnum<PickupMode>("pickupMode", request.PickupMode);
        var pickupAt = request.PickupAt;
        if (pickupMode != PickupMode.Asap && pickupAt is null)
        {
            errors.Add("pickupAt", "Required unless pickupMode is ASAP");
        }

        var command = new CreateTripCommand(
            errors.RequiredText("guestId", request.GuestId),
            errors.RequiredText("propertyId", request.PropertyId),
            errors.RequiredText("pickupLocationId", request.PickupLocationId),
            errors.RequiredText("dropoffLocationId", request.DropoffLocationId),
            pickupAt,
            pickupMode,
            errors.RequiredEnum<ServiceType>("serviceType", request.ServiceType),
            ProductVehicleMapper.Resolve(errors, request.ProductCode, request.VehicleClass, products ?? new SelfServiceOptions()),
            errors.RequiredInt("passengers", request.Passengers),
            errors.RequiredInt("luggage", request.Luggage),
            OptionalText(errors, "specialRequests", request.SpecialRequests),
            errors.RequiredText("confirmationLanguage", request.ConfirmationLanguage is null ? "EN" : request.ConfirmationLanguage),
            idempotencyKey,
            context,
            OptionalText(errors, "couponId", request.CouponId),
            request.ProductCode is null ? null : errors.RequiredEnum<ServiceProduct>("productCode", request.ProductCode));

        return errors.HasErrors
            ? RequestParseResult<CreateTripCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateTripCommand>.Success(command);
    }

    public static RequestParseResult<UpdateTripCommand> ToUpdateCommand(
        string tripId,
        long expectedVersion,
        PatchTripRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var pickupAt = OptionalUtc(errors, "pickupAt", request.PickupAt);
        var command = new UpdateTripCommand(
            tripId,
            expectedVersion,
            errors.PatchText("pickupLocationId", request.PickupLocationId),
            errors.PatchText("dropoffLocationId", request.DropoffLocationId),
            pickupAt,
            errors.PatchEnum<PickupMode>("pickupMode", request.PickupMode),
            errors.PatchEnum<ServiceType>("serviceType", request.ServiceType),
            errors.PatchEnum<VehicleClass>("vehicleClass", request.VehicleClass),
            errors.PatchInt("passengers", request.Passengers),
            errors.PatchInt("luggage", request.Luggage),
            PatchNullableText(errors, "specialRequests", request.SpecialRequests),
            errors.PatchText("confirmationLanguage", request.ConfirmationLanguage),
            context,
            ReadModificationReason(errors, request.ModificationReason),
            OptionalText(errors, "modificationComments", request.ModificationComments));
        if (command.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateTripCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateTripCommand>.Success(command);
    }

    public static RequestParseResult<CancelTripCommand> ToCancelCommand(
        string tripId,
        long expectedVersion,
        CancelTripRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CancelTripCommand(
            tripId,
            expectedVersion,
            errors.RequiredText("reasonCode", request.ReasonCode),
            request.Comments?.Trim() ?? string.Empty,
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CancelTripCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CancelTripCommand>.Success(command);
    }

    private static TripModificationReason? ReadModificationReason(FieldErrorCollector errors, string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (WireEnum.TryParse<TripModificationReason>(value, out var reason))
        {
            return reason;
        }

        errors.Add("modificationReason", $"Must be one of: {string.Join(", ", WireEnum.WireNames<TripModificationReason>())}");

        return null;
    }

    // 可选文本：提供时不得为空白（与查询参数口径一致）。
    private static string? OptionalText(FieldErrorCollector errors, string field, string? value)
    {
        if (value is null)
        {
            return null;
        }

        return errors.RequiredText(field, value);
    }

    // 可清空文本：显式 null 表示清除。
    private static Optional<string?> PatchNullableText(FieldErrorCollector errors, string field, Optional<string?> value) =>
        value.IsSet && value.Value is not null ? Optional<string?>.Of(errors.RequiredText(field, value.Value)) : value;

    private static Optional<DateTimeOffset> OptionalUtc(FieldErrorCollector errors, string field, Optional<DateTimeOffset?> value)
    {
        if (!value.IsSet)
        {
            return default;
        }

        if (value.Value is null)
        {
            errors.Add(field, "Must be an instant, not null");
            return default;
        }

        return Optional<DateTimeOffset>.Of(value.Value.Value);
    }
}

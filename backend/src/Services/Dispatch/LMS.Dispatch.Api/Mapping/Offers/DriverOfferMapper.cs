using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Offers;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Application.Offers.Models;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Api.Mapping.Offers;

internal static class DriverOfferMapper
{
    public static DriverOfferView ToView(DriverOffer offer, DateTimeOffset serverTime) => new(
        offer.OfferId,
        offer.TripId,
        offer.TripReference,
        WireEnum.ToWire(offer.Status),
        offer.ExpiresAt,
        offer.CreatedAt,
        offer.RespondedAt,
        serverTime,
        new OfferTripView(offer.Trip.PickupAt, offer.Trip.PickupName, offer.Trip.DropoffName, offer.Trip.Passengers, offer.Trip.VehicleClass),
        offer.Version);

    public static RequestParseResult<RespondToOfferCommand> ToRespondCommand(
        string offerId,
        string driverId,
        OfferResponseRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var decision = errors.RequiredEnum<OfferDecision>("decision", request.Decision);

        return errors.HasErrors
            ? RequestParseResult<RespondToOfferCommand>.Fail(errors.ToFailure())
            : RequestParseResult<RespondToOfferCommand>.Success(new RespondToOfferCommand(offerId, driverId, decision, context));
    }

    public static RequestParseResult<CreateDriverOfferCommand> ToCreateCommand(
        CreateDriverOfferRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateDriverOfferCommand(
            errors.RequiredText("tripId", request.TripId),
            errors.RequiredText("driverId", request.DriverId),
            idempotencyKey,
            0,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateDriverOfferCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateDriverOfferCommand>.Success(command);
    }
}

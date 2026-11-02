using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.DriverTrips;
using LMS.Booking.Api.Contracts.Trips;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.DriverTrips.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.DriverTrips;

internal static class DriverTripMapper
{
    public static DriverTripSummaryView ToSummary(Trip trip) => new(
        trip.TripId,
        trip.TripReference,
        WireEnum.ToWire(trip.ServiceType),
        WireEnum.ToWire(trip.Status),
        WireEnum.ToWire(TripStages.Of(trip)),
        WireEnum.ToWire(trip.VehicleClass),
        trip.Passengers,
        ToRoute(trip.Route),
        trip.PickupAt,
        trip.Fulfillment.AcceptedAt,
        trip.Fulfillment.PickedUpAt,
        trip.Fulfillment.ArrivedAtDestinationAt,
        trip.Fulfillment.CompletedAt,
        trip.ClosedAt,
        trip.Version);

    public static DriverTripDetailView ToDetail(Trip trip)
    {
        var assignment = trip.Assignment!;

        return new DriverTripDetailView(
            trip.TripId,
            trip.TripReference,
            WireEnum.ToWire(trip.ServiceType),
            WireEnum.ToWire(trip.Status),
            WireEnum.ToWire(TripStages.Of(trip)),
            WireEnum.ToWire(trip.VehicleClass),
            trip.Passengers,
            ToRoute(trip.Route),
            trip.PickupAt,
            trip.ClosedAt,
            new DriverPassengerView(trip.Guest.Name, trip.Guest.PhoneMasked),
            trip.SpecialRequests,
            new DriverAssignmentView(assignment.OfferId, assignment.VehicleLabel),
            ToFulfillment(trip.Fulfillment),
            trip.Events.Select(item => new DriverTripEventView(item.EventId, WireEnum.ToWire(item.Type), WireEnum.ToWire(item.Status), item.OccurredAt)).ToList(),
            FulfillmentRules.AllowedActions(trip).Select(WireEnum.ToWire).ToList(),
            new DriverContactView(false),
            trip.Cancellation is null ? null : new DriverTripCancellationView(trip.Cancellation.ReasonCode, trip.Cancellation.CancelledAt),
            trip.Version);
    }

    public static AssignmentCandidateView ToCandidate(Trip trip) => new(
        trip.TripId,
        trip.TripReference,
        trip.PropertyId,
        trip.CanBeAssigned,
        trip.PickupAt,
        trip.Route.PickupName,
        trip.Route.DropoffName,
        trip.Passengers,
        WireEnum.ToWire(trip.VehicleClass));

    public static TripReferenceView ToReference(Trip trip) => new(trip.TripId, trip.TripReference);

    public static RequestParseResult<RecordTripProgressCommand> ToProgressCommand(
        string tripId,
        string driverId,
        string idempotencyKey,
        ProgressEventRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var action = errors.RequiredEnum<DriverProgressAction>("action", request.Action);

        return errors.HasErrors
            ? RequestParseResult<RecordTripProgressCommand>.Fail(errors.ToFailure())
            : RequestParseResult<RecordTripProgressCommand>.Success(
                new RecordTripProgressCommand(tripId, driverId, action, idempotencyKey, context));
    }

    public static RequestParseResult<AssignDriverCommand> ToAssignCommand(
        string tripId,
        string principalDriverId,
        DriverAssignmentRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var driverId = errors.RequiredText("driverId", request.DriverId);
        if (driverId != principalDriverId)
        {
            errors.Add("driverId", "Must be the driver bound to the caller");
        }

        var command = new AssignDriverCommand(
            tripId,
            errors.RequiredText("offerId", request.OfferId),
            driverId,
            errors.RequiredText("driverName", request.DriverName),
            request.VehicleLabel ?? string.Empty,
            errors.RequiredText("propertyId", request.PropertyId),
            context);

        return errors.HasErrors
            ? RequestParseResult<AssignDriverCommand>.Fail(errors.ToFailure())
            : RequestParseResult<AssignDriverCommand>.Success(command);
    }

    private static TripRouteView ToRoute(TripRoute route) =>
        new(route.PickupLocationId, route.PickupName, route.DropoffLocationId, route.DropoffName, route.IsCrossBorder);

    private static DriverFulfillmentView ToFulfillment(TripFulfillment fulfillment) => new(
        fulfillment.AcceptedAt,
        fulfillment.EnRouteAt,
        fulfillment.ArrivedAtPickupAt,
        fulfillment.PickedUpAt,
        fulfillment.InTransitAt,
        fulfillment.ArrivedAtDestinationAt,
        fulfillment.CompletedAt);
}

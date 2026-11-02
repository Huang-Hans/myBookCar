using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Api.Contracts.ReportingSources;
using LMS.Booking.Application.ReportingSources.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.ReportingSources;

internal static class ReportingTripFactMapper
{
    public static ReportingTripFactsQuery ToQuery(ReportingTripFactsRequest request) =>
        new(request.From!.Value, request.To!.Value, string.IsNullOrWhiteSpace(request.After) ? null : request.After.Trim(), request.Limit);

    public static ReportingTripFactPageView ToView(ReportingTripFactPage page) =>
        new(page.Items.Select(ToView).ToList(), page.NextAfterTripId);

    private static ReportingTripFactView ToView(ReportingTripFact fact)
    {
        var trip = fact.Trip;

        return new ReportingTripFactView(
            trip.TripId,
            trip.TripReference,
            trip.PropertyId,
            trip.Guest.GuestId,
            trip.Guest.Name,
            trip.PickupAt.UtcDateTime,
            WireEnum.ToWire(trip.Status),
            trip.Route.IsCrossBorder,
            trip.Product is null ? null : WireEnum.ToWire(trip.Product.Value),
            new ReportingTripPointsView(trip.Points.Required, trip.Points.Reference, WireEnum.ToWire(trip.Points.State)),
            ToView(trip.Assignment),
            trip.Fulfillment.EnRouteAt?.UtcDateTime,
            trip.Fulfillment.PickedUpAt?.UtcDateTime,
            trip.Fulfillment.CompletedAt?.UtcDateTime,
            ToView(trip.Cancellation),
            fact.PickupToCompleteMeters,
            fact.EnRouteToCompleteMeters);
    }

    private static ReportingTripAssignmentView? ToView(DriverAssignment? assignment) =>
        assignment is null ? null : new ReportingTripAssignmentView(assignment.DriverId, assignment.DriverName, assignment.VehicleLabel);

    private static ReportingTripCancellationView? ToView(TripCancellation? cancellation) =>
        cancellation is null
            ? null
            : new ReportingTripCancellationView(cancellation.ReasonCode, cancellation.CancelledBy, cancellation.CancelledAt.UtcDateTime, cancellation.PolicyReference);
}

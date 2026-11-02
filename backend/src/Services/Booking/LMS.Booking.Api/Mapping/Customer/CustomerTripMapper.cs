using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Api.Contracts.Customer;
using LMS.Booking.Api.Contracts.Trips;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Services;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.Customer;

internal static class CustomerTripMapper
{
    public static CustomerTripSummaryView ToSummary(Trip trip, SelfCancellationRules rules) => new(
        trip.TripId,
        trip.TripReference,
        trip.Product is null ? null : WireEnum.ToWire(trip.Product.Value),
        WireEnum.ToWire(trip.ServiceType),
        WireEnum.ToWire(trip.Status),
        WireEnum.ToWire(GroupOf(trip)),
        WireEnum.ToWire(TripStages.Of(trip)),
        WireEnum.ToWire(trip.VehicleClass),
        trip.Passengers,
        new TripRouteView(trip.Route.PickupLocationId, trip.Route.PickupName, trip.Route.DropoffLocationId, trip.Route.DropoffName, trip.Route.IsCrossBorder),
        trip.PickupAt,
        trip.CreatedAt,
        rules.CanCancel(trip),
        trip.Version);

    public static CustomerTripView ToDetail(Trip trip, SelfCancellationRules rules) => new(
        ToSummary(trip, rules),
        trip.SpecialRequests,
        trip.Assignment is null ? "UNASSIGNED" : "ASSIGNED",
        trip.Assignment is null ? null : new CustomerDriverView(trip.Assignment.DriverName, trip.Assignment.VehicleLabel, CustomerDriverContactRules.CanContact(trip)),
        trip.Events.Select(item => new CustomerTimelineEventView(WireEnum.ToWire(item.Type), WireEnum.ToWire(item.Status), item.OccurredAt)).ToList(),
        new CustomerPointsView(trip.Points.Required, WireEnum.ToWire(trip.Points.State)),
        trip.Cancellation is null ? null : new CustomerCancellationView(trip.Cancellation.ReasonCode, trip.Cancellation.CancelledAt),
        trip.IsTerminal ? RebookInputOf(trip) : null);

    public static CustomerCancellationPolicyView ToPolicy(SelfCancellationPolicy policy) => new(
        policy.Trip.TripId,
        policy.Trip.TripReference,
        WireEnum.ToWire(policy.Trip.Status),
        policy.CanCancel,
        policy.BlockedReason,
        policy.PolicyReference,
        policy.ReasonCode);

    // 客户订单分组：ACTIVE 是全部非终态，不只等于后端 IN_PROGRESS。
    private static GuestTripGroup GroupOf(Trip trip) => trip.Status switch
    {
        TripStatus.Completed => GuestTripGroup.Completed,
        TripStatus.Cancelled => GuestTripGroup.Cancelled,
        _ => GuestTripGroup.Active
    };

    private static CustomerRebookInputView RebookInputOf(Trip trip) => new(
        trip.Product is null ? null : WireEnum.ToWire(trip.Product.Value),
        trip.Route.PickupLocationId,
        trip.Route.DropoffLocationId,
        WireEnum.ToWire(trip.VehicleClass),
        trip.Passengers,
        trip.SpecialRequests);
}

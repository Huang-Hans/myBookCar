using LMS.Dispatch.Domain.Vehicles;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.DispatchOrders;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Application.Orders.Models;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Api.Mapping.DispatchOrders;

internal static class DispatchOrderMapper
{

    public static DispatchOrderView ToView(DispatchOrder item)
    {
        var order = item.Order;

        return new DispatchOrderView(
            order.TripId,
            order.TripReference,
            order.PropertyId,
            WireEnum.ToWire(order.Channel),
            new DispatchOrderGuestView(order.GuestName, order.GuestRoomNumber, order.GuestTierCode, order.GuestMemberId),
            new DispatchOrderRouteView(order.PickupName, order.DropoffName, order.IsCrossBorder),
            order.PickupAt,
            order.PickupMode,
            order.ServiceType,
            order.VehicleClass,
            order.Passengers,
            order.Luggage,
            order.SpecialRequests,
            WireEnum.ToWire(order.Status),
            WireEnum.ToWire(item.Status),
            item.CurrentOffer is null ? null : ToView(item.CurrentOffer, item.SyncedAt),
            order.Assignment is null
                ? null
                : new DispatchOrderAssignmentView(
                    order.Assignment.OfferId, order.Assignment.DriverId, order.Assignment.DriverName, order.Assignment.VehicleLabel, order.Assignment.AssignedAt),
            new DispatchOrderBookedByView(order.BookedBy.UserId, order.BookedBy.RoleCode, order.BookedBy.Name),
            order.CreatedAt,
            item.CanDispatch,
            item.BlockedReason is null ? null : WireEnum.ToWire(item.BlockedReason.Value),
            WireEnum.ToWire(item.Sync),
            item.DispatchVersion,
            item.SyncedAt);
    }

    public static DispatchOfferView ToView(DriverOffer offer, DateTimeOffset serverTime) => new(
        offer.OfferId,
        offer.Round,
        WireEnum.ToWire(offer.Status),
        offer.DriverId,
        offer.DriverName,
        offer.CreatedBy,
        offer.CreatedAt,
        offer.ExpiresAt,
        offer.RespondedAt,
        serverTime,
        offer.Version);

    public static DispatchOrderSummaryView ToView(DispatchOrderSummary summary) => new(
        summary.Total,
        summary.Counts.ToDictionary(pair => WireEnum.ToWire(pair.Key), pair => pair.Value));

    public static DispatchCandidateView ToView(Driver driver) => new(
        driver.DriverId,
        driver.Name,
        driver.PropertyId,
        WireEnum.ToWire(driver.Availability!.Value),
        driver.AssignedVehicle,
        driver.AssignedVehicleGrade is { } grade ? BookingVehicleGrades.ToClass(grade) : null,
        "MATCHED",
        driver.Languages);

    public static ListDispatchOrdersQuery ToListQuery(DispatchOrderListRequest request, CommandContext context) => new(
        request.ToListQuery(),
        new DispatchOrderFilter(request.PropertyId, request.Channel, request.From, request.To, request.DispatchStatus),
        context);

    public static DispatchOrderSummaryQuery ToSummaryQuery(DispatchOrderSummaryRequest request, CommandContext context) => new(
        request.Search.Trim(),
        new DispatchOrderFilter(request.PropertyId, request.Channel, request.From, request.To, null),
        context);

    public static RequestParseResult<CreateDriverOfferCommand> ToCreateCommand(
        string tripId,
        CreateDispatchOfferRequest request,
        string idempotencyKey,
        long expectedRound,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var driverId = errors.RequiredText("driverId", request.DriverId);

        return errors.HasErrors
            ? RequestParseResult<CreateDriverOfferCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateDriverOfferCommand>.Success(
                new CreateDriverOfferCommand(tripId, driverId, idempotencyKey, expectedRound, context));
    }
}

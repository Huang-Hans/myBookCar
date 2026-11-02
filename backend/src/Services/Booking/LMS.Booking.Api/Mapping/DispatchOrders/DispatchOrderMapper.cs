using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.DispatchOrders;
using LMS.Booking.Application.DispatchOrders.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Mapping.DispatchOrders;

internal static class DispatchOrderMapper
{
    // 单次请求携带的订单标识上限：这是请求体积的安全边界，不是业务规则。
    public const int MaxTripIds = 5000;

    public static DispatchOrderView ToView(Trip trip) => new(
        trip.TripId,
        trip.TripReference,
        trip.PropertyId,
        WireEnum.ToWire(trip.Channel),
        new DispatchOrderGuestView(trip.Guest.Name, trip.Guest.RoomNumber, trip.Guest.TierCode, trip.Guest.MemberId),
        new DispatchOrderRouteView(trip.Route.PickupName, trip.Route.DropoffName, trip.Route.IsCrossBorder),
        trip.PickupAt,
        WireEnum.ToWire(trip.PickupMode),
        WireEnum.ToWire(trip.ServiceType),
        WireEnum.ToWire(trip.VehicleClass),
        trip.Passengers,
        trip.Luggage,
        trip.SpecialRequests,
        WireEnum.ToWire(trip.Status),
        trip.Assignment is null
            ? null
            : new DispatchOrderAssignmentView(
                trip.Assignment.OfferId, trip.Assignment.DriverId, trip.Assignment.DriverName, trip.Assignment.VehicleLabel, trip.Assignment.AssignedAt),
        new DispatchOrderBookedByView(trip.BookedBy.UserId, trip.BookedBy.RoleCode, trip.BookedBy.Name),
        trip.CreatedAt,
        trip.Version);

    public static DispatchOrderCountsView ToView(DispatchOrderCounts counts) => new(
        counts.PendingConfirmation,
        counts.Confirmed,
        counts.Assigned,
        counts.Completed,
        counts.Cancelled,
        counts.ConfirmedAwaiting,
        counts.ConfirmedProcessing,
        counts.ConfirmedRedispatch);

    public static RequestParseResult<DispatchOrderFilter> ToFilter(
        DispatchOrderCriteria criteria,
        string search,
        DispatchOrderTripIdsRequest ids)
    {
        var errors = new FieldErrorCollector();
        var include = ReadIds(errors, "includeTripIds", ids.IncludeTripIds);
        var exclude = ReadIds(errors, "excludeTripIds", ids.ExcludeTripIds);

        return errors.HasErrors
            ? RequestParseResult<DispatchOrderFilter>.Fail(errors.ToFailure())
            : RequestParseResult<DispatchOrderFilter>.Success(new DispatchOrderFilter(
                criteria.PropertyId, criteria.Channel, criteria.From, criteria.To, search, criteria.Group, include, exclude));
    }

    public static RequestParseResult<DispatchOrderCountQuery> ToCountQuery(
        DispatchOrderCriteria criteria,
        string search,
        DispatchOrderCountRequest request,
        PropertyScope scope)
    {
        var errors = new FieldErrorCollector();
        var sets = new DispatchOrderTripSets(
            ReadIds(errors, "awaitingTripIds", request.AwaitingTripIds) ?? [],
            ReadIds(errors, "processingTripIds", request.ProcessingTripIds) ?? [],
            ReadIds(errors, "redispatchTripIds", request.RedispatchTripIds) ?? []);
        var filter = new DispatchOrderFilter(
            criteria.PropertyId, criteria.Channel, criteria.From, criteria.To, search, DispatchOrderGroup.Any, null, null);

        return errors.HasErrors
            ? RequestParseResult<DispatchOrderCountQuery>.Fail(errors.ToFailure())
            : RequestParseResult<DispatchOrderCountQuery>.Success(new DispatchOrderCountQuery(scope, filter, sets));
    }

    private static List<string>? ReadIds(FieldErrorCollector errors, string field, List<string?>? values)
    {
        if (values is null)
        {
            return null;
        }

        if (values.Count > MaxTripIds || values.Exists(string.IsNullOrWhiteSpace))
        {
            errors.Add(field, $"Must contain 0 to {MaxTripIds} non-blank trip ids");
            return null;
        }

        return values.Select(value => value!.Trim()).Distinct(StringComparer.Ordinal).ToList();
    }
}

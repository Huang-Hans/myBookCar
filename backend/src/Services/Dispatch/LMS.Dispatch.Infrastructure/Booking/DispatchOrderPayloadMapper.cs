using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Infrastructure.Booking;

// 把 Booking 的响应结构转成 Dispatch 的订单模型。无法识别的状态或渠道说明两边契约不一致，按响应无法理解处理（Booking 不可用）。
internal static class DispatchOrderPayloadMapper
{
    public static BookingOrder ToOrder(DispatchOrderPayload payload) => new(
        payload.TripId,
        payload.TripReference,
        payload.PropertyId,
        Parse<OrderChannel>(payload.Channel, "channel"),
        payload.Guest.Name,
        payload.Guest.RoomNumber,
        payload.Guest.TierCode,
        payload.Guest.MemberId,
        payload.Route.PickupName,
        payload.Route.DropoffName,
        payload.Route.IsCrossBorder,
        payload.PickupAt,
        payload.PickupMode,
        payload.ServiceType,
        payload.VehicleClass,
        payload.Passengers,
        payload.Luggage,
        payload.SpecialRequests,
        Parse<OrderStatus>(payload.Status, "status"),
        payload.Assignment is null
            ? null
            : new BookingOrderAssignment(
                payload.Assignment.OfferId, payload.Assignment.DriverId, payload.Assignment.DriverName,
                payload.Assignment.VehicleLabel, payload.Assignment.AssignedAt),
        new BookingOrderBookedBy(payload.BookedBy.UserId, payload.BookedBy.RoleCode, payload.BookedBy.Name),
        payload.CreatedAt,
        payload.Version);

    public static BookingOrderCounts ToCounts(DispatchOrderCountsPayload payload) => new(
        payload.PendingConfirmation,
        payload.Confirmed,
        payload.Assigned,
        payload.Completed,
        payload.Cancelled,
        payload.ConfirmedAwaiting,
        payload.ConfirmedProcessing,
        payload.ConfirmedRedispatch);

    private static T Parse<T>(string text, string field) where T : struct, Enum =>
        WireEnum.TryParse<T>(text, out var value) ? value : throw new JsonException($"Unknown {field} '{text}' from Booking.");
}

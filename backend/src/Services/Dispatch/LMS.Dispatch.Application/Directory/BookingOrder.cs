using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Directory;

// Booking 提供的派单队列订单。订单状态与指派以 Booking 为准；宾客只含姓名、房号与等级，不含联系电话。
public sealed record BookingOrder(
    string TripId,
    string TripReference,
    string PropertyId,
    OrderChannel Channel,
    string GuestName,
    string? GuestRoomNumber,
    string GuestTierCode,
    string? GuestMemberId,
    string PickupName,
    string DropoffName,
    bool IsCrossBorder,
    DateTimeOffset PickupAt,
    string PickupMode,
    string ServiceType,
    string VehicleClass,
    int Passengers,
    int Luggage,
    string? SpecialRequests,
    OrderStatus Status,
    BookingOrderAssignment? Assignment,
    BookingOrderBookedBy BookedBy,
    DateTimeOffset CreatedAt,
    long Version)
{
    // 候补 / 审批中或已确认且没有权威指派的订单可以派单。
    public bool IsAssignable => Status is (OrderStatus.Pending or OrderStatus.Confirmed) && Assignment is null;
}

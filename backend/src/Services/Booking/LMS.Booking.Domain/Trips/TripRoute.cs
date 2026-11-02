namespace LMS.Booking.Domain.Trips;

// 行程路线。isCrossBorder 一期只做记录（任一端在香港即视为跨境，实施基线 D8），不与跨境审批联动。
public sealed record TripRoute(
    string PickupLocationId,
    string PickupName,
    string DropoffLocationId,
    string DropoffName,
    bool IsCrossBorder);

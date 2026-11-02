using LMS.BuildingBlocks.Core.Querying;

namespace LMS.Booking.Application.Trips.Models;

// 客户本人的订单列表。GuestId 来自令牌绑定，按实际受益宾客归属，不按经办人判断。
public sealed record ListGuestTripsQuery(ListQuery List, string GuestId, GuestTripGroup Group);

using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Models;

// 司机本人的行程列表。DriverId 来自令牌绑定；Status 仅用于 HISTORY 视图的终态筛选（COMPLETED / CANCELLED）。
public sealed record ListDriverTripsQuery(ListQuery List, string DriverId, DriverTripView View, TripStatus? Status);

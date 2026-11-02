using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Models;

// 校验通过的路线：行程路线快照加起终点所在城市（用于跨境方向显示）。
public sealed record CheckedRoute(TripRoute Route, ServiceCity PickupCity, ServiceCity DropoffCity);

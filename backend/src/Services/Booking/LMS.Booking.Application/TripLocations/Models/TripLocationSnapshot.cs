using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Application.TripLocations.Models;

// 查看者订阅时拿到的当前状态：不在共享窗口或尚未收到上报时 Point 为 null。
public sealed record TripLocationSnapshot(string TripId, bool IsSharing, LocationPoint? Point);

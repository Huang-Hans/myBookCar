using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Application.TripLocations.Models;

// 客服查看的行驶轨迹：Points 按序号升序；Sampled 表示点数超过上限而做了等间隔抽样。
public sealed record TripTrack(string TripId, bool IsSharing, bool Sampled, IReadOnlyList<LocationPoint> Points);

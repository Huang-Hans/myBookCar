namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// WatchTrip 返回的当前状态。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="IsSharing">是否处于共享窗口（司机已出发且行程未结束）。</param>
/// <param name="Location">最新位置；不在共享窗口或尚未收到上报时为 null。</param>
public sealed record TripLocationSnapshotView(string TripId, bool IsSharing, TripLocationView? Location);

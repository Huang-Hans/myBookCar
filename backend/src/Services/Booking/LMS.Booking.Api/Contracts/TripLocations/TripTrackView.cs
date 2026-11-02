namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// 客服查看的行驶轨迹。
/// </summary>
/// <param name="TripId">行程标识。</param>
/// <param name="IsSharing">是否处于共享窗口（司机已出发且行程未结束）；为 false 时不会再有新的点。</param>
/// <param name="Sampled">点数超过上限时为 true，表示做了等间隔抽样（保留首尾两点）。</param>
/// <param name="Points">按序号升序的轨迹点；从未出发的行程为空数组。</param>
public sealed record TripTrackView(string TripId, bool IsSharing, bool Sampled, IReadOnlyList<TripLocationView> Points);

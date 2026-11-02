namespace LMS.Booking.Api.Contracts.Approvals;

/// <summary>
/// 行程路线快照：跨境审批抽屉展示的取货点与送达点名称。
/// </summary>
/// <param name="PickupName">取货地点名称。</param>
/// <param name="DropoffName">送达地点名称。</param>
public sealed record RouteSnapshotView(string PickupName, string DropoffName);

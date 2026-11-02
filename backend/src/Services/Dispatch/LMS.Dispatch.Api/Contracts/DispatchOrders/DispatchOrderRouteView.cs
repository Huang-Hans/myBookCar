namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>取送地点的文本展示。</summary>
/// <param name="PickupName">取车地点名称。</param>
/// <param name="DropoffName">送达地点名称。</param>
/// <param name="IsCrossBorder">是否跨境（仅记录）。</param>
public sealed record DispatchOrderRouteView(string PickupName, string DropoffName, bool IsCrossBorder);

namespace LMS.Booking.Api.Contracts.DispatchOrders;

/// <summary>派单队列中的取送地点名称。</summary>
public sealed record DispatchOrderRouteView(string PickupName, string DropoffName, bool IsCrossBorder);

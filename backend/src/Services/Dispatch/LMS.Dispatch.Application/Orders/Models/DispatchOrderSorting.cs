namespace LMS.Dispatch.Application.Orders.Models;

public static class DispatchOrderSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["tripReference", "pickupAt", "orderStatus", "propertyId", "createdAt", "guest.name", "tripId"];

    // 最近创建的订单排在最前；相同创建时间以行程标识稳定排序。
    public const string DefaultSort = "-createdAt,tripId";
    public const string StableField = "tripId";
}

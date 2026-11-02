using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Orders.Models;

// 派单页面看到的订单：Booking 的订单事实 + Dispatch 最近一轮邀请推导出的派单状态。
// DispatchVersion 是最近一轮邀请的轮次（没有邀请为 0），发送邀请时可作为 X-Expected-Version 防止覆盖他人刚发出的一轮。
public sealed record DispatchOrder(
    BookingOrder Order,
    DispatchStatus Status,
    DriverOffer? CurrentOffer,
    bool CanDispatch,
    DispatchBlockedReason? BlockedReason,
    DispatchSyncStatus Sync,
    long DispatchVersion,
    DateTimeOffset SyncedAt);

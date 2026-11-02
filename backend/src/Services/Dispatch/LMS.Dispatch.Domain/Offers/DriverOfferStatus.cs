namespace LMS.Dispatch.Domain.Offers;

// 派单邀请状态。PENDING 待司机响应；ACCEPTING 司机已接受、等待 Booking 权威指派（跨服务处理中，可重试）；
// ACCEPTED / DECLINED / EXPIRED 为终态；SUPERSEDED 表示行程已不可分配（已被指派、取消或状态改变）。
public enum DriverOfferStatus
{
    Pending,
    Accepting,
    Accepted,
    Declined,
    Expired,
    Superseded
}

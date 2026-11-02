namespace LMS.Dispatch.Domain.Coupons;

// 由停用標記、已使用次數與有效期推導，不另存一份狀態。
// 停用優先；已兌滿其次（即使有效期已過）；其後才是即將生效、已過期、生效中。
public enum CouponStatus
{
    Active,
    Upcoming,
    Exhausted,
    Disabled,
    Expired
}

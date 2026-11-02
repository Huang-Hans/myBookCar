namespace LMS.Dispatch.Domain.Coupons;

// 一張優惠券訂單對應一筆核銷。Restored 為 true 表示訂單已取消、次數已退還，同一鍵可以再次核銷。
public sealed record CouponRedemption(string RedemptionKey, string CouponId, bool Restored);

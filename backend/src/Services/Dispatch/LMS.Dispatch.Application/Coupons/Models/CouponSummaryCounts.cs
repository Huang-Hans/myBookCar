namespace LMS.Dispatch.Application.Coupons.Models;

// 對應優惠券管理頂部四張卡片：生效中張數、發行量合計、已使用次數合計、即將生效檔數。
public sealed record CouponSummaryCounts(long Active, long IssuedQuantity, long RedeemedCount, long Upcoming);

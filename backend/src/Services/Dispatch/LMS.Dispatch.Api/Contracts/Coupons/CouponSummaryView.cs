namespace LMS.Dispatch.Api.Contracts.Coupons;

/// <summary>
/// 優惠券管理頂部四張卡片。按呼叫者可見範圍統計，不受列表的券碼、類型與狀態篩選影響。
/// </summary>
/// <param name="Active">生效中的張數。</param>
/// <param name="IssuedQuantity">發行量合計。</param>
/// <param name="RedeemedCount">已使用次數合計。</param>
/// <param name="Upcoming">即將生效的檔數。</param>
public sealed record CouponSummaryView(long Active, long IssuedQuantity, long RedeemedCount, long Upcoming);

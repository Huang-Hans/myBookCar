namespace LMS.Booking.Application.Previews.Models;

// 积分预览（引用 / 预计口径，LMS 不结算）：Available 为会员快照中的积分，没有可信值时为空；
// 差额与预计余额依赖 Available，未知时为空。
public sealed record BookingPreviewPoints(long Required, long? Available, long? Shortfall, long? EstimatedBalanceAfter);

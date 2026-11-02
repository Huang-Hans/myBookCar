namespace LMS.Booking.Application.Directory;

// 某会员当前有效的客户限制（由 Platform 按业务日期判定）。
public sealed record CustomerRestrictionInfo(string MemberId, string Reason);

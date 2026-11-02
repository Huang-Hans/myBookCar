namespace LMS.Platform.Application.CustomerBlacklists.Models;

// 某个会员当前有效的限制（启用且业务日期落在有效期内，含边界日）。
public sealed record CustomerRestriction(string MemberId, string Reason);

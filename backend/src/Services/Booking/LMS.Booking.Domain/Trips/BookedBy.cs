namespace LMS.Booking.Domain.Trips;

// 代订经办人（HA-03 / PM-AD-51）：一律取自令牌解析结果，禁止来自请求正文。
// Name 是下单时从身份解析保存的姓名快照，不随用户后续改名而变化；早于该字段的存量行程为空，不用当前登录人替代。
public sealed record BookedBy(string UserId, string RoleCode, string? Name = null);

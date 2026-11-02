namespace LMS.Booking.Application.Credentials.Models;

// 客户登录所需的最小凭据视图：只供凭据核对使用，不是宾客资料，也不会出现在任何 API 响应中。
// 宾客快照没有启停字段，本期不新增客户启停业务，因此这里没有状态。
public sealed record CustomerCredential(string GuestId, string? PasswordHash);

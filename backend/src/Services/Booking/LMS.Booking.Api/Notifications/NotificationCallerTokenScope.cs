namespace LMS.Booking.Api.Notifications;

// Hub 调用期间没有“当前请求的 Authorization 头”可转发：员工连接的令牌在连接时从协商头或 access_token 取得，
// 之后每次授权解析都显式设置到这里，由 HttpContextCallerToken 优先使用。仅限当前 DI 作用域，不跨请求共享。
internal sealed class NotificationCallerTokenScope
{
    public string? Token { get; set; }
}

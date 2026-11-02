using System.Net.Http.Headers;
using LMS.Booking.Api.Notifications;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Api.Authorization;

// 从当前请求的 Authorization 头取出 Bearer 令牌，仅用于转发给 Platform / Dispatch。
// 实时通知 Hub 调用时没有可转发的请求头，使用连接携带的员工令牌（NotificationCallerTokenScope，仅限当前作用域）。
internal sealed class HttpContextCallerToken(IHttpContextAccessor accessor, NotificationCallerTokenScope hubToken) : ICallerToken
{
    private const string BearerScheme = "Bearer";

    public string? Value
    {
        get
        {
            if (hubToken.Token is not null)
            {
                return hubToken.Token;
            }

            var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();

            return AuthenticationHeaderValue.TryParse(header, out var parsed) && IsBearer(parsed) ? parsed.Parameter : null;
        }
    }

    private static bool IsBearer(AuthenticationHeaderValue value) =>
        string.Equals(value.Scheme, BearerScheme, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value.Parameter);
}

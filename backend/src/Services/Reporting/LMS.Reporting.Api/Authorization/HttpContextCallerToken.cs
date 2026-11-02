using System.Net.Http.Headers;
using LMS.Reporting.Application.Abstractions;

namespace LMS.Reporting.Api.Authorization;

// 从当前请求的 Authorization 头取出 Bearer 令牌，仅用于转发给 Platform。
internal sealed class HttpContextCallerToken(IHttpContextAccessor accessor) : ICallerToken
{
    private const string BearerScheme = "Bearer";

    public string? Value
    {
        get
        {
            var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();

            return AuthenticationHeaderValue.TryParse(header, out var parsed) && IsBearer(parsed) ? parsed.Parameter : null;
        }
    }

    private static bool IsBearer(AuthenticationHeaderValue value) =>
        string.Equals(value.Scheme, BearerScheme, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value.Parameter);
}

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 取得当前 Hub 连接请求所携带的访问令牌：协商请求用 Authorization 头，WebSocket 用 access_token 查询参数。
// 仅用于员工连接在有效期内重新核验授权。
public static class NotificationAccessTokens
{
    private const string BearerScheme = "Bearer";
    private const string QueryKey = "access_token";

    public static string? Read(HttpContext? context)
    {
        if (context is null)
        {
            return null;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(header, out var parsed)
            && string.Equals(parsed.Scheme, BearerScheme, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(parsed.Parameter))
        {
            return parsed.Parameter;
        }

        var query = context.Request.Query[QueryKey].ToString();

        return string.IsNullOrWhiteSpace(query) ? null : query;
    }
}

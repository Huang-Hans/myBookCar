using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace LMS.ServiceDefaults.Middleware;

// 实时通知 Hub 的 WebSocket / 长轮询把访问令牌放在 access_token 查询参数里（浏览器无法设置 Authorization 头）。
// 链路追踪默认会记录请求的查询串与出站完整地址，这里把含 access_token 的查询串整体遮蔽，令牌不进入任何追踪或日志。
internal static class TokenQueryRedaction
{
    private const string TokenKey = "access_token";
    private const string Redacted = "access_token=[REDACTED]";

    public static void RedactRequest(Activity activity, HttpRequest request)
    {
        if (request.Query.ContainsKey(TokenKey))
        {
            activity.SetTag("url.query", Redacted);
        }
    }

    public static void RedactOutbound(Activity activity, HttpRequestMessage request)
    {
        var uri = request.RequestUri;
        if (uri is null || !uri.Query.Contains(TokenKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var redactedUri = new UriBuilder(uri) { Query = Redacted }.Uri.ToString();
        activity.SetTag("url.full", redactedUri);
        activity.SetTag("url.query", Redacted);
    }
}

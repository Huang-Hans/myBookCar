using LMS.BuildingBlocks.Core.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LMS.BuildingBlocks.Identity.Authentication;

// 浏览器的 WebSocket / 长轮询无法设置 Authorization 头，SignalR 客户端把令牌放在 access_token 查询参数。
// 只对三个实时通知 Hub 的精确路径（及其协商路径）读取，且只在请求没有 Authorization 头时读取；其余路径一律不接受查询串令牌。
// 令牌验证仍走原有方案的全部校验。
internal static class HubAccessTokenReader
{
    private const string QueryKey = "access_token";

    public static Task OnMessageReceivedAsync(MessageReceivedContext context)
    {
        var request = context.HttpContext.Request;
        if (!NotificationHubRoutes.IsHubPath(request.Path.Value) || request.Headers.ContainsKey("Authorization"))
        {
            return Task.CompletedTask;
        }

        var token = request.Query[QueryKey].ToString();
        if (!string.IsNullOrWhiteSpace(token))
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    }
}

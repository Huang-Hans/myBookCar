using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.Booking.Application.Directory;

namespace LMS.Booking.Api.Notifications;

// 客服 Hub 的授权：每次都用连接携带的员工令牌向 Platform 重新解析当前授权，不复用旧缓存，
// 也不依赖只在 MVC 管道里生效的 RequirePermission / RequireCaller 属性。
// 接收客服下单通知需要同时拥有通知、派单与订单的读取权限；物业范围来自当前解析结果。
internal sealed class StaffHubAuthorizer(IPlatformDirectory directory, NotificationCallerTokenScope tokenScope)
{
    private static readonly string[] RequiredPermissions =
        [PermissionCodes.NotificationsRead, PermissionCodes.DispatchRead, PermissionCodes.BookingsRead];

    // 未关联、已停用或权限不足返回 null；Platform 暂时不可用抛出 NotificationVerificationUnavailableException。
    public async Task<PlatformCaller?> ResolveAsync(string? accessToken, CancellationToken cancellationToken)
    {
        tokenScope.Token = accessToken;
        var lookup = await directory.ResolveCallerAsync(cancellationToken);
        if (lookup.Outcome == CallerLookupOutcome.SourceUnavailable)
        {
            throw new NotificationVerificationUnavailableException("The Platform authorization source is unavailable.");
        }

        return lookup.Caller is { } caller && RequiredPermissions.All(caller.Permissions.Contains) ? caller : null;
    }
}

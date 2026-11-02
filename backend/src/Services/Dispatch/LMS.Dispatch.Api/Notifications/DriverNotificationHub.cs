using System.Security.Claims;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.Dispatch.Application.Notifications.Services;
using Microsoft.AspNetCore.Authorization;

namespace LMS.Dispatch.Api.Notifications;

/// <summary>
/// 司机实时通知 Hub（/api/dispatch/driver/v1/notifications）：连接后只加入以令牌 sub（driverId）确定的个人组，
/// 接收新邀请、邀请关闭与接受成功事件。停用或暂停的司机不能建立连接。没有任何客户端可调用的业务方法：
/// 接受 / 拒绝仍通过既有 HTTP 接口，202 ACCEPTING 是处理中，只有 ACCEPTED 才是接受完成。
/// </summary>
[Authorize(Policy = AuthorizationPolicies.Driver)]
internal sealed class DriverNotificationHub(NotificationConnectionRegistry registry, DriverNotificationAccess access)
    : NotificationHubBase(registry)
{
    protected override async Task<NotificationConnectionAccess?> AuthorizeConnectionAsync(CancellationToken cancellationToken)
    {
        var driverId = Context.User?.FindFirstValue(AppTokenClaims.Subject);
        if (string.IsNullOrWhiteSpace(driverId) || !await access.IsUsableAsync(driverId, cancellationToken))
        {
            return null;
        }

        return new NotificationConnectionAccess(RealtimeRecipientKind.Driver, driverId);
    }
}

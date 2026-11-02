using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.Dispatch.Application.Notifications.Services;

namespace LMS.Dispatch.Api.Notifications;

// 司机在线连接的授权重新核验：司机记录仍存在且为启用状态；停用 / 暂停后连接被关闭，不再保有接单消息订阅。
internal sealed class DispatchNotificationConnectionVerifier(DriverNotificationAccess drivers) : INotificationConnectionVerifier
{
    public Task<bool> IsStillAuthorizedAsync(NotificationConnectionAccess access, CancellationToken cancellationToken) =>
        access.Kind == RealtimeRecipientKind.Driver
            ? drivers.IsUsableAsync(access.RecipientId, cancellationToken)
            : Task.FromResult(false);
}

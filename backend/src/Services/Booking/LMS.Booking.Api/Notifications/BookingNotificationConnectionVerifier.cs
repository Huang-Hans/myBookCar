using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Realtime.Hubs;

namespace LMS.Booking.Api.Notifications;

// 在线连接的授权重新核验：员工用连接令牌重新向 Platform 解析当前授权（停用、权限收回都会撤销），
// 客户核对本地记录与登录条件。Platform 暂时不可用时抛出 NotificationVerificationUnavailableException，连接保持。
internal sealed class BookingNotificationConnectionVerifier(StaffHubAuthorizer staff, CustomerHubAuthorizer customers) : INotificationConnectionVerifier
{
    public async Task<bool> IsStillAuthorizedAsync(NotificationConnectionAccess access, CancellationToken cancellationToken)
    {
        switch (access.Kind)
        {
            case RealtimeRecipientKind.Staff:
                var caller = await staff.ResolveAsync(access.AccessToken, cancellationToken);
                return caller is not null && caller.UserId == access.RecipientId;
            case RealtimeRecipientKind.Customer:
                return await customers.IsUsableAsync(access.RecipientId, cancellationToken);
            default:
                return false;
        }
    }
}

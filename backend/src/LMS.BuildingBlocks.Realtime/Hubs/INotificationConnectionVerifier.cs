namespace LMS.BuildingBlocks.Realtime.Hubs;

// 在线连接的授权重新核验。返回 false 表示接收人已不再有效（停用、权限或物业范围变化），连接应被关闭；
// 数据源暂时不可用时抛出 NotificationVerificationUnavailableException，连接保持到下个周期或令牌到期。
public interface INotificationConnectionVerifier
{
    Task<bool> IsStillAuthorizedAsync(NotificationConnectionAccess access, CancellationToken cancellationToken);
}

using Microsoft.AspNetCore.SignalR;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 受守卫管理的一条在线连接：令牌到期时间与最近一次核验时间。
public sealed class NotificationTrackedConnection(HubCallerContext context, NotificationConnectionAccess access, DateTimeOffset? expiresAt, DateTimeOffset verifiedAt)
{
    public HubCallerContext Context { get; } = context;

    public NotificationConnectionAccess Access { get; } = access;

    public DateTimeOffset? ExpiresAt { get; } = expiresAt;

    public DateTimeOffset VerifiedAt { get; set; } = verifiedAt;

    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt is { } expiresAt && now >= expiresAt;
}

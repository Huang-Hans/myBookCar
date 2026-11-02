using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 本进程内的在线连接清单，供连接守卫处理令牌到期与授权重新核验。不用于定位收件人：投递只走个人组。
public sealed class NotificationConnectionRegistry(TimeProvider timeProvider)
{
    private const string ExpirationClaim = "exp";

    private readonly ConcurrentDictionary<string, NotificationTrackedConnection> connections = new(StringComparer.Ordinal);

    public void Add(HubCallerContext context, NotificationConnectionAccess access)
    {
        var tracked = new NotificationTrackedConnection(context, access, ExpirationOf(context), timeProvider.GetUtcNow());
        connections[context.ConnectionId] = tracked;
    }

    public void Remove(string connectionId) => connections.TryRemove(connectionId, out _);

    public NotificationTrackedConnection? Find(string connectionId) =>
        connections.TryGetValue(connectionId, out var tracked) ? tracked : null;

    public IReadOnlyList<NotificationTrackedConnection> Snapshot() => connections.Values.ToList();

    private static DateTimeOffset? ExpirationOf(HubCallerContext context) =>
        long.TryParse(context.User?.FindFirst(ExpirationClaim)?.Value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
}

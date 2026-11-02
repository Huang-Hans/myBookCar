using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Wire;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 个人接收组：{接收端类型}:{实体标识}。同一个字符串标识在不同接收端类型下是不同的组。
public static class NotificationHubGroups
{
    public static string For(RealtimeRecipientKind kind, string recipientId) =>
        $"{WireEnum.ToWire(kind).ToLowerInvariant()}:{recipientId}";
}

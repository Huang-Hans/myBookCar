using LMS.BuildingBlocks.Core.Wire;

namespace LMS.BuildingBlocks.Core.Notifications.Events;

// 由业务事实推导稳定的事件标识：evt_{事件类型}_{业务对象...}[_{接收人}]。重试和重复登记得到同一个值。
public static class NotificationEventIds
{
    private const string Prefix = "evt_";

    public static string For(RealtimeEventType type, params string[] parts) =>
        Prefix + string.Join('_', [WireEnum.ToWire(type).ToLowerInvariant(), .. parts]);
}

namespace LMS.BuildingBlocks.Realtime.Mongo;

// 待发送事件的技术集合（Booking、Dispatch 各在自己的库中一份，互不访问）。
// 主键即事件标识，同一业务事实只会登记一次。后台只领取 Pending 且到期的事件；
// 清理 TTL 只作用于已进入终态并写入 purgeAt 的事件，未处理的事件没有 purgeAt，因此不会被清理。
// 索引由 backend/migrations 中各服务的 Flyway 脚本发布，API 启动时不建索引。
public static class NotificationEventCollection
{
    public const string Name = "notification_events";
}

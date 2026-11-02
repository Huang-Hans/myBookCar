namespace LMS.BuildingBlocks.Core.Notifications.Events;

// 待发送事件的处理状态。Pending 是唯一会被后台领取的状态；其余都是终态。
// Published 只表示已交给 Hub，不表示在线、展示、已读或接单。Materialized 只用于按物业扇出的源事件。
public enum NotificationEventState
{
    Pending,
    Published,
    Materialized,
    Skipped,
    Failed
}

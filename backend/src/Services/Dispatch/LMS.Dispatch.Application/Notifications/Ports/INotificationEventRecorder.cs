using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Notifications.Ports;

// 在业务事务内登记一条待发送事件。登记与业务事实同一个本地事务提交：事务回滚时没有事件，
// 事务提交时事件一定存在，因此不会出现“业务已生效但没有通知记录”的双写缺口。
// 同一业务事实的事件标识固定，重复登记会违反主键并抛出 DuplicateRecordException。
public interface INotificationEventRecorder
{
    Task RecordAsync(NotificationEventDraft draft, ITransactionContext context, CancellationToken cancellationToken);
}

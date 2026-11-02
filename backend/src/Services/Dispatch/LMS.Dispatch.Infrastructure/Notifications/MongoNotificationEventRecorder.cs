using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.BuildingBlocks.MongoDB.Errors;
using LMS.BuildingBlocks.Realtime.Mongo;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Notifications.Ports;
using LMS.Dispatch.Infrastructure.Transactions;

namespace LMS.Dispatch.Infrastructure.Notifications;

// 在调用方的本地事务里写 notification_events：与业务事实同提交、同回滚。
// 主键即事件标识，同一业务事实重复登记会违反主键，转换为 DuplicateRecordException，与其他仓储一致。
internal sealed class MongoNotificationEventRecorder(MongoNotificationOutbox outbox, TimeProvider timeProvider) : INotificationEventRecorder
{
    public async Task RecordAsync(NotificationEventDraft draft, ITransactionContext context, CancellationToken cancellationToken)
    {
        try
        {
            await outbox.InsertAsync(MongoTransactionContext.RequireSession(context), draft, null, timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (Exception exception) when (MongoDuplicateKey.IsDuplicateKey(exception))
        {
            throw new DuplicateRecordException(NotificationEventCollection.Name, exception);
        }
    }
}

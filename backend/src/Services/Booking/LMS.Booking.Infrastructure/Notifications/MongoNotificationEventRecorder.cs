using LMS.BuildingBlocks.MongoDB.Errors;
using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.BuildingBlocks.Realtime.Mongo;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Notifications.Ports;
using LMS.Booking.Infrastructure.Transactions;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Infrastructure.Notifications;

// 在调用方的本地事务里写 notification_events：与业务事实同提交、同回滚。
// 主键即事件标识，同一业务事实重复登记会违反主键，转换为 DuplicateRecordException，与其他仓储一致。
internal sealed class MongoNotificationEventRecorder(
    MongoNotificationOutbox outbox,
    IOptions<NotificationDeliveryOptions> options,
    TimeProvider timeProvider) : INotificationEventRecorder, IStaffFanOutStore
{
    public Task RecordAsync(NotificationEventDraft draft, ITransactionContext context, CancellationToken cancellationToken) =>
        InsertAsync(draft, null, context, cancellationToken);

    public Task RecordPersonalAsync(NotificationEventDraft draft, string notificationId, ITransactionContext context, CancellationToken cancellationToken) =>
        InsertAsync(draft, notificationId, context, cancellationToken);

    public Task<IReadOnlySet<string>> FindExistingAsync(
        IReadOnlyCollection<string> eventIds,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        outbox.FindExistingIdsAsync(MongoTransactionContext.RequireSession(context), eventIds, cancellationToken);

    public Task<bool> CompleteSourceAsync(string sourceEventId, string leaseOwner, ITransactionContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        return outbox.MarkMaterializedAsync(
            MongoTransactionContext.RequireSession(context), sourceEventId, leaseOwner, now, now.AddDays(options.Value.RetentionDays), cancellationToken);
    }

    private async Task InsertAsync(NotificationEventDraft draft, string? notificationId, ITransactionContext context, CancellationToken cancellationToken)
    {
        try
        {
            await outbox.InsertAsync(MongoTransactionContext.RequireSession(context), draft, notificationId, timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (Exception exception) when (MongoDuplicateKey.IsDuplicateKey(exception))
        {
            throw new DuplicateRecordException(NotificationEventCollection.Name, exception);
        }
    }
}

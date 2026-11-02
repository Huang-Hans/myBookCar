using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.ConsoleNotifications.Ports;
using LMS.Booking.Application.Notifications.Ports;
using LMS.Booking.Domain.Notifications;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Notifications.Services;

// 订单提交后的后台任务：向 Platform 受限目录取得物业范围内的授权客服（含离线客服），
// 在 Booking 本地事务内为每位收件人创建一次站内通知和一条个人发送事件，最后把源事件置为已物化。
// 目录失败时源事件保持待处理并重试，不会按“零收件人”成功结束；分批物化，每位收件人的标识稳定，重试不会产生重复未读通知。
internal sealed class StaffNotificationFanOut(
    IStaffNotificationDirectory directory,
    ITransactionRunner transactions,
    IStaffFanOutStore store,
    IConsoleNotificationRepository notifications,
    IOptions<ConsoleOptions> consoleOptions,
    TimeProvider timeProvider) : INotificationFanOut
{
    private const int BatchSize = 100;
    private const string DirectoryUnavailable = "DIRECTORY_UNAVAILABLE";
    private const string LeaseLost = "LEASE_LOST";

    public async Task<NotificationDeliveryResult> MaterializeAsync(NotificationEvent source, string leaseOwner, CancellationToken cancellationToken)
    {
        var recipients = await directory.ListRecipientsAsync(source.Draft.EventTypeWire, source.Draft.PropertyId!, cancellationToken);
        if (!recipients.IsSuccess)
        {
            return NotificationDeliveryResult.Retry(DirectoryUnavailable);
        }

        foreach (var batch in recipients.Value!.Distinct(StringComparer.Ordinal).Chunk(BatchSize))
        {
            await transactions.ExecuteAsync((context, token) => MaterializeBatchAsync(source.Draft, batch, context, token), cancellationToken);
        }

        var completed = await transactions.ExecuteAsync(
            (context, token) => store.CompleteSourceAsync(source.EventId, leaseOwner, context, token), cancellationToken);

        return completed ? NotificationDeliveryResult.Delivered : NotificationDeliveryResult.Retry(LeaseLost);
    }

    private async Task<bool> MaterializeBatchAsync(
        NotificationEventDraft source,
        IReadOnlyList<string> userIds,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var drafts = userIds.Select(userId => BookingNotificationEvents.PersonalOf(source, userId)).ToList();
        var existing = await store.FindExistingAsync(drafts.Select(draft => draft.EventId).ToList(), context, cancellationToken);
        foreach (var draft in drafts.Where(draft => !existing.Contains(draft.EventId)))
        {
            await MaterializeOneAsync(draft, context, cancellationToken);
        }

        return true;
    }

    private async Task MaterializeOneAsync(NotificationEventDraft draft, ITransactionContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var notificationId = BookingNotificationEvents.NotificationIdOf(draft);
        var notification = ConsoleNotification.Materialized(
            notificationId,
            draft.RecipientId!,
            draft.Summary,
            draft.TripReference,
            draft.OccurredAt,
            now.Add(Retention()),
            new NotificationTarget(draft.TargetType, draft.TargetId),
            new NotificationSource(draft.EventTypeWire, draft.PropertyId!, draft.EventId));

        await notifications.InsertAsync(notification, context, cancellationToken);
        await store.RecordPersonalAsync(draft, notificationId, context, cancellationToken);
    }

    private TimeSpan Retention() =>
        consoleOptions.Value.NotificationRetentionDays > 0 ? TimeSpan.FromDays(consoleOptions.Value.NotificationRetentionDays) : TimeSpan.FromDays(3650);
}

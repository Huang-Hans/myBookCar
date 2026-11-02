using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Core.Notifications.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Realtime.Delivery;

// 一次处理周期：领取到期事件，按扇出源事件或个人事件分别处理，并把结果按租约写回。
// 发布失败不回滚已完成的业务：保留事件、退避重试，达到上限记录为失败，不会被写成已发布。
// 崩溃重试允许重复发布，事件标识保持不变，客户端据此去重。
public sealed class NotificationDispatcher(
    INotificationOutbox outbox,
    INotificationDeliverer deliverer,
    IOptions<NotificationDeliveryOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationDispatcher> logger,
    INotificationFanOut? fanOut = null)
{
    private const string UnexpectedFailure = "DELIVERY_ERROR";
    private const string ValidityElapsed = "VALIDITY_ELAPSED";

    public async Task<int> RunOnceAsync(string leaseOwner, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var claimed = await outbox.ClaimDueAsync(
            leaseOwner, timeProvider.GetUtcNow(), TimeSpan.FromSeconds(settings.LeaseSeconds), settings.BatchSize, cancellationToken);
        foreach (var notificationEvent in claimed)
        {
            await ProcessAsync(notificationEvent, leaseOwner, cancellationToken);
        }

        return claimed.Count;
    }

    private async Task ProcessAsync(NotificationEvent notificationEvent, string leaseOwner, CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["traceId"] = notificationEvent.Draft.TraceId,
            ["eventId"] = notificationEvent.EventId
        });
        var result = await ResolveAsync(notificationEvent, leaseOwner, cancellationToken);
        await RecordAsync(notificationEvent, leaseOwner, result, cancellationToken);
    }

    private async Task<NotificationDeliveryResult> ResolveAsync(NotificationEvent notificationEvent, string leaseOwner, CancellationToken cancellationToken)
    {
        try
        {
            if (HasElapsed(notificationEvent))
            {
                return NotificationDeliveryResult.Skipped(ValidityElapsed);
            }

            return IsFanOutSource(notificationEvent)
                ? await fanOut!.MaterializeAsync(notificationEvent, leaseOwner, cancellationToken)
                : await deliverer.DeliverAsync(notificationEvent, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Publishing notification event {EventId} failed.", notificationEvent.EventId);

            return NotificationDeliveryResult.Retry(UnexpectedFailure);
        }
    }

    private bool IsFanOutSource(NotificationEvent notificationEvent) => notificationEvent.Draft.IsPropertyFanOut && fanOut is not null;

    // 已过有效期的新邀请事件不再提示；司机重新查询现有邀请接口即可。
    private bool HasElapsed(NotificationEvent notificationEvent) =>
        notificationEvent.Draft.ValidUntil is { } until && timeProvider.GetUtcNow() >= until;

    private async Task RecordAsync(
        NotificationEvent notificationEvent,
        string leaseOwner,
        NotificationDeliveryResult result,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var purgeAt = now.AddDays(options.Value.RetentionDays);
        var written = result.Outcome switch
        {
            NotificationDeliveryOutcome.Delivered => await MarkDeliveredAsync(notificationEvent, leaseOwner, now, purgeAt, cancellationToken),
            NotificationDeliveryOutcome.Skipped => await outbox.MarkSkippedAsync(notificationEvent.EventId, leaseOwner, result.Reason!, purgeAt, cancellationToken),
            _ => await RetryAsync(notificationEvent, leaseOwner, result.Reason!, now, purgeAt, cancellationToken)
        };
        if (!written)
        {
            logger.LogInformation("Notification event {EventId} result was discarded because its lease changed.", notificationEvent.EventId);
        }
    }

    // 扇出源事件由物化事务自己置为 Materialized，这里只处理个人事件。
    private Task<bool> MarkDeliveredAsync(
        NotificationEvent notificationEvent,
        string leaseOwner,
        DateTimeOffset now,
        DateTimeOffset purgeAt,
        CancellationToken cancellationToken) =>
        IsFanOutSource(notificationEvent)
            ? Task.FromResult(true)
            : outbox.MarkPublishedAsync(notificationEvent.EventId, leaseOwner, now, purgeAt, cancellationToken);

    private Task<bool> RetryAsync(
        NotificationEvent notificationEvent,
        string leaseOwner,
        string reason,
        DateTimeOffset now,
        DateTimeOffset purgeAt,
        CancellationToken cancellationToken)
    {
        var attempts = notificationEvent.Attempts + 1;
        if (NotificationRetryPolicy.IsExhausted(options.Value, attempts))
        {
            return outbox.MarkFailedAsync(notificationEvent.EventId, leaseOwner, reason, purgeAt, cancellationToken);
        }

        var nextAttemptAt = NotificationRetryPolicy.NextAttemptAt(options.Value, attempts, now);

        return outbox.RescheduleAsync(notificationEvent.EventId, leaseOwner, attempts, nextAttemptAt, reason, cancellationToken);
    }
}

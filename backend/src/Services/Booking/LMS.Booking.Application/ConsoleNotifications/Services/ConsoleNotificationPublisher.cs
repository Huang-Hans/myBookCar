using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.ConsoleNotifications.Ports;
using LMS.Booking.Domain.Notifications;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.ConsoleNotifications.Services;

// 站内通知发布器：事务内重载供建单/取消/推进复用（同事务提交，不产生孤儿通知）；
// 独立重载用于决定类工作流之后补发（幂等重放不重发）。
internal sealed class ConsoleNotificationPublisher(
    IConsoleNotificationRepository notifications,
    ITransactionRunner transactionRunner,
    IOptions<ConsoleOptions> consoleOptions,
    TimeProvider timeProvider)
{
    // 保留天数关闭时给远期过期，等价于不清理（TTL 索引仍在，仅时间兜底）。
    private static readonly TimeSpan DisabledRetention = TimeSpan.FromDays(3650);

    public Task PublishAsync(
        string recipientUserId,
        NotificationType type,
        string summary,
        string? tripReference,
        NotificationTarget? target,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var (now, expiresAt) = NowAndExpiry();

        return notifications.InsertAsync(
            ConsoleNotification.Create(recipientUserId, type, summary, tripReference, now, expiresAt, target),
            context,
            cancellationToken);
    }

    public async Task PublishStandaloneAsync(
        string recipientUserId,
        NotificationType type,
        string summary,
        string? tripReference,
        NotificationTarget? target,
        CancellationToken cancellationToken)
    {
        var (now, expiresAt) = NowAndExpiry();

        await transactionRunner.ExecuteAsync(
            (context, token) => WrapAsync(notifications.InsertAsync(
                ConsoleNotification.Create(recipientUserId, type, summary, tripReference, now, expiresAt, target), context, token)),
            cancellationToken);
    }

    private (DateTimeOffset Now, DateTimeOffset ExpiresAt) NowAndExpiry()
    {
        var now = timeProvider.GetUtcNow();
        var retention = consoleOptions.Value.NotificationRetentionDays > 0
            ? TimeSpan.FromDays(consoleOptions.Value.NotificationRetentionDays)
            : DisabledRetention;

        return (now, now.Add(retention));
    }

    private static async Task<byte> WrapAsync(Task insert)
    {
        await insert;
        return 0;
    }
}

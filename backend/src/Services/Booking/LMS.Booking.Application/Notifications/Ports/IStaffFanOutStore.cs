using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Notifications.Ports;

// 按物业扇出的物化事务所需的事件存储操作，与站内通知写入处于同一个本地事务。
public interface IStaffFanOutStore
{
    // 这批个人事件中已经登记过的事件标识（重试时据此跳过，保证每位收件人只物化一次）。
    Task<IReadOnlySet<string>> FindExistingAsync(IReadOnlyCollection<string> eventIds, ITransactionContext context, CancellationToken cancellationToken);

    Task RecordPersonalAsync(NotificationEventDraft draft, string notificationId, ITransactionContext context, CancellationToken cancellationToken);

    // 把仍由本租约持有者领取的源事件置为已物化；租约已被接管时返回 false。
    Task<bool> CompleteSourceAsync(string sourceEventId, string leaseOwner, ITransactionContext context, CancellationToken cancellationToken);
}

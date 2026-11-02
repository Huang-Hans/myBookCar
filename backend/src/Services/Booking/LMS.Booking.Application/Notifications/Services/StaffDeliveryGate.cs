using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.Booking.Application.Notifications.Ports;

namespace LMS.Booking.Application.Notifications.Services;

// 员工事件发布前的授权复核：重新取得同一物业的当前授权收件人，与已物化的收件人取交集。
// 原收件人已被停用、权限收回或不再属于该物业时不投递业务内容；目录暂时不可用时稍后重试，而不是放行。
// 同一处理周期内每个物业只查询一次。
public sealed class StaffDeliveryGate(IStaffNotificationDirectory directory)
{
    private const string DirectoryUnavailable = "DIRECTORY_UNAVAILABLE";
    private const string RecipientNotAuthorized = "RECIPIENT_NOT_AUTHORIZED";

    private readonly Dictionary<string, IReadOnlySet<string>> authorizedByProperty = new(StringComparer.Ordinal);

    // 返回 null 表示允许投递；否则返回应记录的结论。
    public async Task<NotificationDeliveryResult?> CheckAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken)
    {
        var draft = notificationEvent.Draft;
        var authorized = await AuthorizedAsync(draft.EventTypeWire, draft.PropertyId!, cancellationToken);
        if (authorized is null)
        {
            return NotificationDeliveryResult.Retry(DirectoryUnavailable);
        }

        return authorized.Contains(draft.RecipientId!) ? null : NotificationDeliveryResult.Skipped(RecipientNotAuthorized);
    }

    private async Task<IReadOnlySet<string>?> AuthorizedAsync(string eventType, string propertyId, CancellationToken cancellationToken)
    {
        if (authorizedByProperty.TryGetValue(propertyId, out var cached))
        {
            return cached;
        }

        var result = await directory.ListRecipientsAsync(eventType, propertyId, cancellationToken);
        if (!result.IsSuccess)
        {
            return null;
        }

        var recipients = result.Value!.ToHashSet(StringComparer.Ordinal);
        authorizedByProperty[propertyId] = recipients;

        return recipients;
    }
}

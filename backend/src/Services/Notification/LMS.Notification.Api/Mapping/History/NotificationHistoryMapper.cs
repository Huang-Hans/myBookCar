using LMS.BuildingBlocks.Core.Wire;
using LMS.Notification.Api.Contracts.History;
using LMS.Notification.Application.History.Models;
using LMS.Notification.Domain.History;

namespace LMS.Notification.Api.Mapping.History;

internal static class NotificationHistoryMapper
{
    public static ListNotificationHistoryQuery ToListQuery(NotificationHistoryListRequest request) =>
        new(request.ToListQuery(), request.Status, request.From, request.To);

    public static NotificationHistoryView ToView(NotificationHistoryEntry entry) => new(
        entry.NotificationId,
        entry.Recipient,
        WireEnum.ToWire(entry.Channel),
        entry.OccurredAt.UtcDateTime,
        WireEnum.ToWire(entry.EventType),
        WireEnum.ToWire(entry.Status),
        entry.ContentPreview);
}

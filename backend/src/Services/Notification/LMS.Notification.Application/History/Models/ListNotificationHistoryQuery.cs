using LMS.BuildingBlocks.Core.Querying;
using LMS.Notification.Domain.History;

namespace LMS.Notification.Application.History.Models;

// From 含，To 不含。
public sealed record ListNotificationHistoryQuery(ListQuery List, NotificationStatus? Status, DateTimeOffset? From, DateTimeOffset? To);

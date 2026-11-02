namespace LMS.BuildingBlocks.Core.Notifications.Contracts;

// 通知内容。所有键都出现在线上，可空字段显式为 null（Hub 序列化不省略 null）。
// notificationId / type 只有客服的站内通知有值（type 固定 SYSTEM_ALERT）；司机与客户为 null。
// targetType 与 targetId 同时有值或同时为 null。
public sealed record NotificationContentV1(
    string? NotificationId,
    string? Type,
    string Summary,
    string? TripReference,
    DateTime OccurredAt,
    string? TargetType,
    string? TargetId);

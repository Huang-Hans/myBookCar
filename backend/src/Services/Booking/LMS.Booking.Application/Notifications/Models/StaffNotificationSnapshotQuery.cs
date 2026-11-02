using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.Notifications.Models;

// 客服通知快照查询：本人 + 当前物业范围（来自重新解析的授权）+ 分页。
public sealed record StaffNotificationSnapshotQuery(string RecipientUserId, PropertyScope Scope, int Page, int PageSize);

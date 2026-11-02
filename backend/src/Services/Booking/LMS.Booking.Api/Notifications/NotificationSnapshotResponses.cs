using System.Diagnostics;
using LMS.BuildingBlocks.Core.Notifications.Contracts;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.Booking.Application.Notifications.Models;
using LMS.Booking.Domain.Notifications;

namespace LMS.Booking.Api.Notifications;

// getNotificationSnapshot 的结果：沿用统一 Envelope（成功 ApiEnvelope，失败 ApiErrorEnvelope）。
// Hub 方法结果中的 code 只是业务结果码，不等同于一次新的 HTTP 状态码。
internal static class NotificationSnapshotResponses
{
    public static string NewTraceId() => ActivityTraceId.CreateRandom().ToHexString();

    public static ApiEnvelope<NotificationSnapshotDataV1> Success(StaffNotificationSnapshot snapshot, NotificationSnapshotRequest request, string traceId)
    {
        var totalItems = (int)Math.Min(snapshot.TotalItems, int.MaxValue);
        var pagination = new ApiPagination(request.Page, request.PageSize, totalItems, (int)Math.Ceiling(totalItems / (double)request.PageSize));
        var data = new NotificationSnapshotDataV1(snapshot.Items.Select(ToItem).ToList(), snapshot.UnreadCount);

        return new ApiEnvelope<NotificationSnapshotDataV1>(NotificationMessageV1.SuccessCode, NotificationMessageV1.SuccessMessage, data, new ApiMeta(traceId, pagination), traceId);
    }

    public static ApiErrorEnvelope Failure(ApiError error, string traceId) => Failure(error, [], traceId);

    public static ApiErrorEnvelope Failure(ApiError error, IReadOnlyList<ApiFieldError> errors, string traceId) =>
        new(error.Code, error.Message, errors, traceId);

    public static ApiErrorEnvelope InvalidRequest(string traceId) =>
        Failure(
            ApiErrors.ValidationFailed,
            [
                new ApiFieldError("page", "Must be 1 or greater"),
                new ApiFieldError("pageSize", $"Must be between 1 and {NotificationSnapshotRequest.MaxPageSize}")
            ],
            traceId);

    private static NotificationSnapshotItemV1 ToItem(ConsoleNotification notification)
    {
        var source = notification.Source!;
        var content = NotificationMessageFactory.ContentOf(
            notification.NotificationId,
            notification.Summary,
            notification.TripReference,
            notification.OccurredAt,
            notification.Target?.Type,
            notification.Target?.Id);

        return new NotificationSnapshotItemV1(source.SourceEventId, source.EventType, content, notification.IsRead, notification.ReadAt?.UtcDateTime);
    }
}

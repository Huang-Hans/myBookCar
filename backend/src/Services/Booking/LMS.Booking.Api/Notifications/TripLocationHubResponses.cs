using LMS.BuildingBlocks.Core.Notifications.Contracts;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.Booking.Api.Contracts.TripLocations;
using LMS.Booking.Api.Mapping.TripLocations;
using LMS.Booking.Application.TripLocations.Models;
using Microsoft.AspNetCore.SignalR;

namespace LMS.Booking.Api.Notifications;

// WatchTrip / UnwatchTrip 的结果：与 getNotificationSnapshot 一样返回统一外壳，不抛出连接异常。
// 无权查看与行程不存在不作区分，一律 40400；只有校验通过才加入行程组。
internal static class TripLocationHubResponses
{
    public static async Task<object> JoinAsync(
        IGroupManager groups,
        string connectionId,
        OperationResult<TripLocationSnapshot> result,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            return NotificationSnapshotResponses.Failure(ErrorOf(result.Outcome), traceId);
        }

        await groups.AddToGroupAsync(connectionId, TripLocationGroups.For(result.Value!.TripId), cancellationToken);

        return new ApiEnvelope<TripLocationSnapshotView>(
            NotificationMessageV1.SuccessCode, NotificationMessageV1.SuccessMessage, TripLocationMapper.ToView(result.Value), new ApiMeta(traceId), traceId);
    }

    public static async Task<object> LeaveAsync(IGroupManager groups, string connectionId, string tripId, string traceId, CancellationToken cancellationToken)
    {
        await groups.RemoveFromGroupAsync(connectionId, TripLocationGroups.For(tripId), cancellationToken);

        return new ApiEnvelope<object>(NotificationMessageV1.SuccessCode, NotificationMessageV1.SuccessMessage, null, new ApiMeta(traceId), traceId);
    }

    public static ApiErrorEnvelope InvalidTripId(string traceId) =>
        NotificationSnapshotResponses.Failure(ApiErrors.ValidationFailed, [new ApiFieldError("tripId", "Required")], traceId);

    private static ApiError ErrorOf(OperationOutcome outcome) =>
        outcome == OperationOutcome.NotFound ? ApiErrors.ResourceNotFound : ApiErrors.ServiceUnavailable;
}

using System.Security.Claims;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.Booking.Application.TripLocations.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace LMS.Booking.Api.Notifications;

/// <summary>
/// 客户实时通知 Hub（/api/booking/customer/v1/notifications）：连接后只加入以令牌 sub（guestId）确定的个人组，
/// 接收订单指派与行程状态变化事件。只读方法 WatchTrip / UnwatchTrip 订阅本人行程的实时位置（客户端方法 tripLocationUpdated），
/// 没有任何下单、修改或取消方法。
/// </summary>
[Authorize(Policy = AuthorizationPolicies.Customer)]
internal sealed class CustomerNotificationHub(
    NotificationConnectionRegistry registry,
    CustomerHubAuthorizer authorizer,
    ITripLocationWatchHandler locationWatches,
    TimeProvider timeProvider) : NotificationHubBase(registry)
{
    /// <summary>订阅本人行程的实时位置并返回当前快照；不是本人的行程或不存在返回 40400，且不加入订阅。</summary>
    public async Task<object> WatchTrip(string? tripId)
    {
        var traceId = NotificationSnapshotResponses.NewTraceId();
        if (string.IsNullOrWhiteSpace(tripId))
        {
            return TripLocationHubResponses.InvalidTripId(traceId);
        }

        var failure = CheckConnection(traceId);
        if (failure is not null)
        {
            return failure;
        }

        var result = await locationWatches.GetForCustomerAsync(tripId, CurrentAccess!.RecipientId, Context.ConnectionAborted);

        return await TripLocationHubResponses.JoinAsync(Groups, Context.ConnectionId, result, traceId, Context.ConnectionAborted);
    }

    /// <summary>退订行程位置；总是成功。</summary>
    public async Task<object> UnwatchTrip(string? tripId)
    {
        var traceId = NotificationSnapshotResponses.NewTraceId();

        return string.IsNullOrWhiteSpace(tripId)
            ? TripLocationHubResponses.InvalidTripId(traceId)
            : await TripLocationHubResponses.LeaveAsync(Groups, Context.ConnectionId, tripId, traceId, Context.ConnectionAborted);
    }

    protected override async Task<NotificationConnectionAccess?> AuthorizeConnectionAsync(CancellationToken cancellationToken)
    {
        var guestId = Context.User?.FindFirstValue(AppTokenClaims.Subject);
        if (string.IsNullOrWhiteSpace(guestId) || !await authorizer.IsUsableAsync(guestId, cancellationToken))
        {
            return null;
        }

        return new NotificationConnectionAccess(RealtimeRecipientKind.Customer, guestId);
    }

    private ApiErrorEnvelope? CheckConnection(string traceId)
    {
        var connection = CurrentConnection;
        if (connection is null)
        {
            return NotificationSnapshotResponses.Failure(ApiErrors.NotAuthenticated, traceId);
        }

        return connection.IsExpiredAt(timeProvider.GetUtcNow())
            ? NotificationSnapshotResponses.Failure(ApiErrors.TokenExpired, traceId)
            : null;
    }
}

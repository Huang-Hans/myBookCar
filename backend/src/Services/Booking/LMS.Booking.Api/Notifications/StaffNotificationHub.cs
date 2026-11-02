using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Notifications.Contracts;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Notifications.Handlers;
using LMS.Booking.Application.Notifications.Models;
using LMS.Booking.Application.TripLocations.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;

namespace LMS.Booking.Api.Notifications;

/// <summary>
/// 客服实时通知 Hub（/api/booking/staff/v1/notifications）：连接时由服务端把员工解析为 Platform 本地 userId 并加入个人组，
/// 只推送本人可见物业范围内的客户自助下单通知。客户端方法 notificationReceived 接收 NotificationMessageV1；
/// 只读方法 getNotificationSnapshot 返回本人下单通知快照与总未读数，用于首次加载、断线恢复和角标同步；
/// 只读方法 WatchTrip / UnwatchTrip 订阅物业范围内行程的实时位置（客户端方法 tripLocationUpdated）。
/// Hub 不提供下单、派单、接单或任意订阅方法；标记已读继续使用既有 HTTP 接口。
/// </summary>
[Authorize(Policy = AuthorizationPolicies.Staff)]
internal sealed class StaffNotificationHub(
    NotificationConnectionRegistry registry,
    StaffHubAuthorizer authorizer,
    IStaffNotificationSnapshotHandler snapshots,
    ITripLocationWatchHandler locationWatches,
    TimeProvider timeProvider) : NotificationHubBase(registry)
{
    protected override async Task<NotificationConnectionAccess?> AuthorizeConnectionAsync(CancellationToken cancellationToken)
    {
        var token = NotificationAccessTokens.Read(Context.GetHttpContext());
        var caller = await authorizer.ResolveAsync(token, cancellationToken);

        return caller is null ? null : new NotificationConnectionAccess(RealtimeRecipientKind.Staff, caller.UserId, token);
    }

    /// <summary>本人当前可见的客户下单通知（page 默认 1，pageSize 默认 20、1～100）与总未读数；旧审批、取消等通知不包含在内。</summary>
    public async Task<object> GetNotificationSnapshot(NotificationSnapshotRequest? request)
    {
        var traceId = NotificationSnapshotResponses.NewTraceId();
        var query = request ?? new NotificationSnapshotRequest();
        if (!query.IsValid)
        {
            return NotificationSnapshotResponses.InvalidRequest(traceId);
        }

        var (caller, failure) = await ResolveCallerAsync(traceId, Context.ConnectionAborted);
        if (failure is not null)
        {
            return failure;
        }

        var snapshot = await snapshots.GetAsync(
            new StaffNotificationSnapshotQuery(caller!.UserId, ScopeOf(caller), query.Page, query.PageSize), Context.ConnectionAborted);

        return NotificationSnapshotResponses.Success(snapshot, query, traceId);
    }

    /// <summary>订阅物业范围内行程的实时位置并返回当前快照；范围外或不存在返回 40400，且不加入订阅。</summary>
    public async Task<object> WatchTrip(string? tripId)
    {
        var traceId = NotificationSnapshotResponses.NewTraceId();
        if (string.IsNullOrWhiteSpace(tripId))
        {
            return TripLocationHubResponses.InvalidTripId(traceId);
        }

        var (caller, failure) = await ResolveCallerAsync(traceId, Context.ConnectionAborted);
        if (failure is not null)
        {
            return failure;
        }

        var result = await locationWatches.GetForStaffAsync(tripId, ScopeOf(caller!), Context.ConnectionAborted);

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

    private static PropertyScope ScopeOf(PlatformCaller caller) =>
        caller.HasAllPropertyAccess ? PropertyScope.AllProperties : PropertyScope.ForProperty(caller.PropertyId!);

    // 每次都用连接携带的员工令牌向 Platform 重新解析当前授权，不复用连接时的结果。
    private async Task<(PlatformCaller? Caller, ApiErrorEnvelope? Failure)> ResolveCallerAsync(string traceId, CancellationToken cancellationToken)
    {
        var failure = CheckConnection(traceId);
        if (failure is not null)
        {
            return (null, failure);
        }

        try
        {
            var caller = await authorizer.ResolveAsync(CurrentAccess!.AccessToken, cancellationToken);

            return caller is null ? (null, NotificationSnapshotResponses.Failure(ApiErrors.InsufficientPermission, traceId)) : (caller, null);
        }
        catch (NotificationVerificationUnavailableException)
        {
            return (null, NotificationSnapshotResponses.Failure(ApiErrors.ServiceUnavailable, traceId));
        }
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

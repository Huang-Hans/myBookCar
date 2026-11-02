using System.Diagnostics;
using LMS.BuildingBlocks.Core.Notifications.Contracts;
using LMS.Booking.Api.Contracts.TripLocations;
using LMS.Booking.Api.Mapping.TripLocations;
using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Domain.TripLocations;
using Microsoft.AspNetCore.SignalR;

namespace LMS.Booking.Api.Notifications;

// 把行程位置推送到客户 Hub 与客服 Hub 的行程组。发送成功只表示交给了 Hub（空组也算）；
// 失败只记日志、不重试，不影响已保存的位置或已提交的行程结果。
internal sealed class SignalRTripLocationPublisher(
    IHubContext<CustomerNotificationHub> customerHub,
    IHubContext<StaffNotificationHub> staffHub,
    ILogger<SignalRTripLocationPublisher> logger) : ITripLocationPublisher
{
    private const int SchemaVersion = 1;
    private const string UpdatedEvent = "TRIP_LOCATION_UPDATED";
    private const string EndedEvent = "TRIP_LOCATION_ENDED";

    public Task PublishUpdatedAsync(string tripId, LocationPoint point, CancellationToken cancellationToken) =>
        SendAsync(tripId, new TripLocationMessageData(SchemaVersion, UpdatedEvent, tripId, TripLocationMapper.ToView(point)), cancellationToken);

    public Task PublishEndedAsync(string tripId, CancellationToken cancellationToken) =>
        SendAsync(tripId, new TripLocationMessageData(SchemaVersion, EndedEvent, tripId, null), cancellationToken);

    private async Task SendAsync(string tripId, TripLocationMessageData data, CancellationToken cancellationToken)
    {
        var message = new TripLocationMessage(
            NotificationMessageV1.SuccessCode, NotificationMessageV1.SuccessMessage, data, ActivityTraceId.CreateRandom().ToHexString());
        var group = TripLocationGroups.For(tripId);
        try
        {
            await Task.WhenAll(
                customerHub.Clients.Group(group).SendAsync(TripLocationGroups.ClientMethod, message, cancellationToken),
                staffHub.Clients.Group(group).SendAsync(TripLocationGroups.ClientMethod, message, cancellationToken));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Trip location {EventType} was not pushed for trip {TripId}.", data.EventType, tripId);
        }
    }
}

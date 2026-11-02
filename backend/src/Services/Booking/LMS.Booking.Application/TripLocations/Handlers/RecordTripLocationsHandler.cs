using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Application.TripLocations.Commands;
using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Domain.TripLocations;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.TripLocations.Handlers;

// 司机上报定位：只接受本人行程在共享窗口内的点。整批点先追加到行驶轨迹（按序号去重），再把序号最大的一个保存为最新位置；
// 先写轨迹保证推送出去的点都能在轨迹接口中查到。新位置写入后按行程限流推送给正在查看的客户与客服。重复补传的旧点被忽略，接口仍成功，便于司机端安全重试。
internal sealed class RecordTripLocationsHandler(
    IDriverTripRepository driverTrips,
    ITripLiveLocationRepository locations,
    ITripLocationTrackRepository track,
    ITripLocationPublisher publisher,
    TimeProvider timeProvider) : IRecordTripLocationsHandler
{
    // 设备时钟允许比服务端快的上限；更晚的定位时间视为无效数据。
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    // 同一行程两次推送的最小间隔。
    private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(3);

    public async Task<OperationResult<long>> HandleAsync(RecordTripLocationsCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (command.Points.Any(point => point.RecordedAt > now + ClockSkew))
        {
            return OperationResult<long>.Invalid([new FieldIssue("points", "recordedAt must not be later than the server time")]);
        }

        var trip = await driverTrips.FindForDriverAsync(command.TripId, command.DriverId, cancellationToken);
        if (trip is null)
        {
            return OperationResult<long>.Failed(OperationOutcome.NotFound);
        }

        if (!TripLocationSharing.IsSharing(trip))
        {
            return OperationResult<long>.Failed(OperationOutcome.StateConflict);
        }

        return await RecordAcceptedAsync(trip, command, now, cancellationToken);
    }

    // 已通过校验的一批点：先追加轨迹，再条件写入最新位置并按需推送；返回服务端已保存的最大序号。
    private async Task<OperationResult<long>> RecordAcceptedAsync(Trip trip, RecordTripLocationsCommand command, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await track.AppendAsync(trip.TripId, command.Points, now, now + TripLocationTrack.Retention, cancellationToken);
        var latest = command.Points.MaxBy(point => point.Seq)!;
        var candidate = new TripLiveLocation(trip.TripId, command.DriverId, trip.PropertyId, latest, now, null, now + TripLocationSharing.Retention);
        var saved = await locations.SaveIfNewerAsync(candidate, cancellationToken);
        await PushIfDueAsync(saved, latest, now, cancellationToken);

        return OperationResult<long>.Succeeded(saved.Point.Seq);
    }

    // 只推送本次真正写入的新位置；距上次推送不足间隔时跳过，下一次满足间隔的上报会带上最新位置。
    private async Task PushIfDueAsync(TripLiveLocation saved, LocationPoint latest, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (saved.Point.Seq != latest.Seq)
        {
            return;
        }

        if (await locations.TryClaimPushAsync(saved.TripId, now - PushInterval, now, cancellationToken))
        {
            await publisher.PublishUpdatedAsync(saved.TripId, saved.Point, cancellationToken);
        }
    }
}

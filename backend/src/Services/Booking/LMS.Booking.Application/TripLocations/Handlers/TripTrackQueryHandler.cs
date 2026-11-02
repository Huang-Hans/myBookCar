using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.TripLocations.Models;
using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Application.TripLocations.Handlers;

// 归属规则与客服行程详情接口、WatchTrip 一致。已结束的行程在保留期内仍返回完整轨迹，从未出发的返回空轨迹。
internal sealed class TripTrackQueryHandler(
    ITripRepository trips,
    ITripLocationTrackRepository track) : ITripTrackQueryHandler
{
    public async Task<OperationResult<TripTrack>> GetForStaffAsync(string tripId, long afterSeq, PropertyScope scope, CancellationToken cancellationToken)
    {
        var trip = await trips.FindVisibleAsync(tripId, scope, cancellationToken);
        if (trip is null)
        {
            return OperationResult<TripTrack>.Failed(OperationOutcome.NotFound);
        }

        var points = await track.ListAsync(trip.TripId, afterSeq, cancellationToken);
        var result = new TripTrack(
            trip.TripId,
            TripLocationSharing.IsSharing(trip),
            TripLocationTrack.NeedsSampling(points),
            TripLocationTrack.Sample(points));

        return OperationResult<TripTrack>.Succeeded(result);
    }
}

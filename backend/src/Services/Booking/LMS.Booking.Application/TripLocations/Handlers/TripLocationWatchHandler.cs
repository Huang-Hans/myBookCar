using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.TripLocations.Models;
using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.TripLocations;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.TripLocations.Handlers;

// 归属规则与现有客户行程接口、客服行程详情接口一致；不在共享窗口时不返回位置。
internal sealed class TripLocationWatchHandler(
    ITripRepository trips,
    IGuestTripRepository guestTrips,
    ITripLiveLocationRepository locations) : ITripLocationWatchHandler
{
    public async Task<OperationResult<TripLocationSnapshot>> GetForCustomerAsync(string tripId, string guestId, CancellationToken cancellationToken) =>
        await SnapshotOfAsync(tripId, await guestTrips.FindForGuestAsync(tripId, guestId, cancellationToken), cancellationToken);

    public async Task<OperationResult<TripLocationSnapshot>> GetForStaffAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken) =>
        await SnapshotOfAsync(tripId, await trips.FindVisibleAsync(tripId, scope, cancellationToken), cancellationToken);

    private async Task<OperationResult<TripLocationSnapshot>> SnapshotOfAsync(string tripId, Trip? trip, CancellationToken cancellationToken)
    {
        if (trip is null)
        {
            return OperationResult<TripLocationSnapshot>.Failed(OperationOutcome.NotFound);
        }

        if (!TripLocationSharing.IsSharing(trip))
        {
            return OperationResult<TripLocationSnapshot>.Succeeded(new TripLocationSnapshot(tripId, false, null));
        }

        var location = await locations.FindAsync(tripId, cancellationToken);

        return OperationResult<TripLocationSnapshot>.Succeeded(new TripLocationSnapshot(tripId, true, location?.Point));
    }
}

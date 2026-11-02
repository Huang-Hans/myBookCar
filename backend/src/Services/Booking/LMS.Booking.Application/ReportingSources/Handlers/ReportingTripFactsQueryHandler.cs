using LMS.Booking.Application.ReportingSources.Models;
using LMS.Booking.Application.ReportingSources.Ports;
using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Domain.TripLocations;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.ReportingSources.Handlers;

// 按批读取行程，并一次性读取这批行程的行驶轨迹来计算距离；轨迹已过保留期或从未上报时距离为 null。
internal sealed class ReportingTripFactsQueryHandler(
    IReportingTripRepository trips,
    ITripLocationTrackRepository track) : IReportingTripFactsQueryHandler
{
    public async Task<ReportingTripFactPage> ListAsync(ReportingTripFactsQuery query, CancellationToken cancellationToken)
    {
        var batch = await trips.ListByPickupAsync(query.From, query.To, query.AfterTripId, query.Limit, cancellationToken);
        var points = await track.ListForTripsAsync(batch.Where(HasTrack).Select(trip => trip.TripId).ToList(), cancellationToken);
        var facts = batch.Select(trip => ToFact(trip, points.GetValueOrDefault(trip.TripId, []))).ToList();
        var nextAfter = batch.Count < query.Limit ? null : batch[^1].TripId;

        return new ReportingTripFactPage(facts, nextAfter);
    }

    // 只有完成的行程才有完整的里程区间。
    private static bool HasTrack(Trip trip) => trip.Fulfillment.CompletedAt is not null;

    private static ReportingTripFact ToFact(Trip trip, IReadOnlyList<LocationPoint> points)
    {
        var fulfillment = trip.Fulfillment;

        return new ReportingTripFact(
            trip,
            TripTrackDistance.MetersWithin(points, fulfillment.PickedUpAt, fulfillment.CompletedAt),
            TripTrackDistance.MetersWithin(points, fulfillment.EnRouteAt, fulfillment.CompletedAt));
    }
}

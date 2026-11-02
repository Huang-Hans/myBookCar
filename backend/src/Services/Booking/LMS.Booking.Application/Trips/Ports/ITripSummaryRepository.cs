using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Ports;

public interface ITripSummaryRepository
{
    // 各状态的行程数，八个状态恒全量返回（没有行程为 0）。
    Task<IReadOnlyDictionary<TripStatus, long>> CountByStatusAsync(TripSummaryQuery query, CancellationToken cancellationToken);
}

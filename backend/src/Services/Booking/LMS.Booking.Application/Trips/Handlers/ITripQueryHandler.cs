using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Handlers;

public interface ITripQueryHandler
{
    Task<OperationResult<PageResult<Trip>>> ListAsync(ListTripsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<Trip>> GetAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken);

    Task<OperationResult<TripCancellationPolicy>> GetCancellationPolicyAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyDictionary<TripStatus, long>>> SummariesAsync(TripSummaryQuery query, CancellationToken cancellationToken);
}

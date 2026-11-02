using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Handlers;

internal sealed class TripQueryHandler(
    ITripRepository trips,
    ITripSummaryRepository summaries,
    IPlatformCatalog catalog) : ITripQueryHandler
{
    public async Task<OperationResult<PageResult<Trip>>> ListAsync(ListTripsQuery query, CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<Trip>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<Trip>>.Succeeded(await trips.ListAsync(query, cancellationToken));
    }

    public async Task<OperationResult<Trip>> GetAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var trip = await trips.FindVisibleAsync(tripId, scope, cancellationToken);

        return trip is null
            ? OperationResult<Trip>.Failed(OperationOutcome.NotFound)
            : OperationResult<Trip>.Succeeded(trip);
    }

    // 取消前政策展示（HA-10）：行程归属物业的政策引用；Platform 不可用 → 503，不给默认文案。
    public async Task<OperationResult<TripCancellationPolicy>> GetCancellationPolicyAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var trip = await trips.FindVisibleAsync(tripId, scope, cancellationToken);
        if (trip is null)
        {
            return OperationResult<TripCancellationPolicy>.Failed(OperationOutcome.NotFound);
        }

        var property = await catalog.GetPropertyAsync(trip.PropertyId, cancellationToken);
        if (!property.IsSuccess)
        {
            return property.Outcome == DirectoryOutcome.Forbidden
                ? OperationResult<TripCancellationPolicy>.Failed(OperationOutcome.Forbidden)
                : OperationResult<TripCancellationPolicy>.Failed(OperationOutcome.SourceUnavailable);
        }

        return OperationResult<TripCancellationPolicy>.Succeeded(
            new TripCancellationPolicy(trip.TripReference, trip.PropertyId, trip.Status, property.Value!.CancellationPolicyReference));
    }

    public async Task<OperationResult<IReadOnlyDictionary<TripStatus, long>>> SummariesAsync(TripSummaryQuery query, CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<IReadOnlyDictionary<TripStatus, long>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<IReadOnlyDictionary<TripStatus, long>>.Succeeded(await summaries.CountByStatusAsync(query, cancellationToken));
    }
}

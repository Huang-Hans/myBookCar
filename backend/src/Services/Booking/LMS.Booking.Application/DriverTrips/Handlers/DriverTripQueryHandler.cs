using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Handlers;

internal sealed class DriverTripQueryHandler(IDriverTripRepository driverTrips, ITripRepository trips) : IDriverTripQueryHandler
{
    public async Task<OperationResult<PageResult<Trip>>> ListAsync(ListDriverTripsQuery query, CancellationToken cancellationToken) =>
        OperationResult<PageResult<Trip>>.Succeeded(await driverTrips.ListForDriverAsync(query, cancellationToken));

    public async Task<OperationResult<Trip>> GetAsync(string tripId, string driverId, CancellationToken cancellationToken) =>
        OrNotFound(await driverTrips.FindForDriverAsync(tripId, driverId, cancellationToken));

    public async Task<OperationResult<Trip>> GetActiveAsync(string driverId, CancellationToken cancellationToken) =>
        OrNotFound(await driverTrips.FindActiveForDriverAsync(driverId, cancellationToken));

    public async Task<OperationResult<Trip>> GetForAssignmentAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken) =>
        OrNotFound(await trips.FindVisibleAsync(tripId, scope, cancellationToken));

    private static OperationResult<Trip> OrNotFound(Trip? trip) =>
        trip is null ? OperationResult<Trip>.Failed(OperationOutcome.NotFound) : OperationResult<Trip>.Succeeded(trip);
}

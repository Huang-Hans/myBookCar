using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Handlers;

public interface IUpdateTripHandler
{
    Task<OperationResult<Trip>> HandleAsync(UpdateTripCommand command, CancellationToken cancellationToken);
}

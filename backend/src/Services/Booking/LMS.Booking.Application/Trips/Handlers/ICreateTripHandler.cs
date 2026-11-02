using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Handlers;

public interface ICreateTripHandler
{
    Task<OperationResult<Trip>> HandleAsync(CreateTripCommand command, CancellationToken cancellationToken);
}

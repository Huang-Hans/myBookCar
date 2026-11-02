using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Handlers;

public interface ICancelTripHandler
{
    Task<OperationResult<Trip>> HandleAsync(CancelTripCommand command, CancellationToken cancellationToken);
}

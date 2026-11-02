using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Handlers;

public interface IAdvanceTripStatusHandler
{
    Task<OperationResult<Trip>> HandleAsync(AdvanceTripStatusCommand command, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.DriverTrips.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Handlers;

public interface IAssignDriverHandler
{
    Task<OperationResult<Trip>> HandleAsync(AssignDriverCommand command, CancellationToken cancellationToken);
}

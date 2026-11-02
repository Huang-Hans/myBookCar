using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.DriverTrips.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Handlers;

public interface IRecordTripProgressHandler
{
    Task<OperationResult<Trip>> HandleAsync(RecordTripProgressCommand command, CancellationToken cancellationToken);
}

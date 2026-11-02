using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Commands;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Handlers;

public interface ICreateSelfServiceTripHandler
{
    Task<OperationResult<Trip>> HandleAsync(CreateSelfServiceTripCommand command, CancellationToken cancellationToken);
}

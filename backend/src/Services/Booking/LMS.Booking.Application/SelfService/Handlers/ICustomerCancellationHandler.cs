using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Commands;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Handlers;

public interface ICustomerCancellationHandler
{
    Task<OperationResult<SelfCancellationPolicy>> GetPolicyAsync(string tripId, string guestId, CancellationToken cancellationToken);

    Task<OperationResult<Trip>> CancelAsync(CancelSelfServiceTripCommand command, CancellationToken cancellationToken);
}

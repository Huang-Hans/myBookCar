using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Guests.Commands;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.Guests.Handlers;

public interface IUpdateGuestHandler
{
    Task<OperationResult<GuestProfile>> UpdateAsync(UpdateGuestCommand command, CancellationToken cancellationToken);
}

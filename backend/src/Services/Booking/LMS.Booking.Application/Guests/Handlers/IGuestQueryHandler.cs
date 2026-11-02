using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Guests.Models;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.Guests.Handlers;

public interface IGuestQueryHandler
{
    Task<OperationResult<PageResult<GuestProfile>>> SearchAsync(ListGuestsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<GuestProfileBundle>> GetProfileAsync(string guestId, PropertyScope scope, CancellationToken cancellationToken);

    Task<OperationResult<GuestQualificationCheck>> CheckQualificationAsync(
        string guestId,
        string propertyId,
        PropertyScope scope,
        CommandContext context,
        CancellationToken cancellationToken);
}

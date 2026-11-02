using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Models;

namespace LMS.Booking.Application.SelfService.Handlers;

public interface ICustomerOfferingsHandler
{
    Task<OperationResult<ServiceOfferings>> GetAsync(string guestId, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Models;

namespace LMS.Booking.Application.SelfService.Handlers;

public interface ICustomerProfileHandler
{
    Task<OperationResult<CustomerSummary>> GetAsync(string guestId, CancellationToken cancellationToken);
}

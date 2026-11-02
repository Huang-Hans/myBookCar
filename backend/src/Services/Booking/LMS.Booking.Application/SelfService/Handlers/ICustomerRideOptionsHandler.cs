using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Models;

namespace LMS.Booking.Application.SelfService.Handlers;

public interface ICustomerRideOptionsHandler
{
    Task<OperationResult<RideOptionsResult>> GetAsync(
        string guestId,
        string productCode,
        string pickupLocationId,
        string dropoffLocationId,
        int passengers,
        CancellationToken cancellationToken);
}

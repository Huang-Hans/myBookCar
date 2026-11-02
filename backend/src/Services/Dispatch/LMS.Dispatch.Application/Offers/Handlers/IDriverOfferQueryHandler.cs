using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Handlers;

public interface IDriverOfferQueryHandler
{
    Task<OperationResult<PageResult<DriverOffer>>> ListAsync(string driverId, ListQuery list, CancellationToken cancellationToken);
}

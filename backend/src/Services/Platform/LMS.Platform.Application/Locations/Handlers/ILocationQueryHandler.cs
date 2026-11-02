using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Handlers;

public interface ILocationQueryHandler
{
    Task<OperationResult<PageResult<CommonLocation>>> ListAsync(ListLocationsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<CommonLocation>> GetAsync(string locationId, CancellationToken cancellationToken);
}

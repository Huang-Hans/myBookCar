using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Application.Locations.Ports;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Handlers;

internal sealed class LocationQueryHandler(ILocationRepository locations) : ILocationQueryHandler
{
    public async Task<OperationResult<PageResult<CommonLocation>>> ListAsync(
        ListLocationsQuery query,
        CancellationToken cancellationToken) =>
        OperationResult<PageResult<CommonLocation>>.Succeeded(await locations.ListAsync(query, cancellationToken));

    public async Task<OperationResult<CommonLocation>> GetAsync(string locationId, CancellationToken cancellationToken)
    {
        var location = await locations.FindByIdAsync(locationId, cancellationToken);

        return location is null
            ? OperationResult<CommonLocation>.Failed(OperationOutcome.NotFound)
            : OperationResult<CommonLocation>.Succeeded(location);
    }
}

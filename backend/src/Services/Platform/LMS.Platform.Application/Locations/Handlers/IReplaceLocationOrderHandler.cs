using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Locations.Commands;
using LMS.Platform.Application.Locations.Models;

namespace LMS.Platform.Application.Locations.Handlers;

public interface IReplaceLocationOrderHandler
{
    Task<OperationResult<LocationOrdering>> HandleAsync(ReplaceLocationOrderCommand command, CancellationToken cancellationToken);
}

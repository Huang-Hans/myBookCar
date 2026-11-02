using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Locations.Commands;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Handlers;

public interface IUpdateLocationHandler
{
    Task<OperationResult<CommonLocation>> HandleAsync(UpdateLocationCommand command, CancellationToken cancellationToken);
}

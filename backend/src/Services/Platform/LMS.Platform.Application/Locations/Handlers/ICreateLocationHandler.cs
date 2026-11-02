using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Locations.Commands;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Handlers;

public interface ICreateLocationHandler
{
    Task<OperationResult<CommonLocation>> HandleAsync(CreateLocationCommand command, CancellationToken cancellationToken);
}

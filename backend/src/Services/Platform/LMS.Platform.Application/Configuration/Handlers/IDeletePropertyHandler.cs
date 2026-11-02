using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Configuration.Commands;

namespace LMS.Platform.Application.Configuration.Handlers;

public interface IDeletePropertyHandler
{
    Task<OperationResult<bool>> HandleAsync(DeletePropertyCommand command, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Drivers.Commands;

namespace LMS.Dispatch.Application.Drivers.Handlers;

public interface IRemoveDriverHandler
{
    Task<OperationResult<bool>> HandleAsync(RemoveDriverCommand command, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

public interface IUpdateDriverHandler
{
    Task<OperationResult<Driver>> HandleAsync(UpdateDriverCommand command, CancellationToken cancellationToken);
}

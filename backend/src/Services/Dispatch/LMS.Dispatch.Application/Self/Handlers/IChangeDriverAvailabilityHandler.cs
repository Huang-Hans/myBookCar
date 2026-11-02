using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Self.Commands;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Self.Handlers;

public interface IChangeDriverAvailabilityHandler
{
    Task<OperationResult<Driver>> HandleAsync(ChangeAvailabilityCommand command, CancellationToken cancellationToken);
}

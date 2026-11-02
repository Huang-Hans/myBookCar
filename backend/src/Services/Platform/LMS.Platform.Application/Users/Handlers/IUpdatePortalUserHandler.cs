using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Users.Commands;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Handlers;

public interface IUpdatePortalUserHandler
{
    Task<OperationResult<PortalUser>> HandleAsync(UpdatePortalUserCommand command, CancellationToken cancellationToken);
}

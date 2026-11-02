using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Users.Commands;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Handlers;

public interface ICreatePortalUserHandler
{
    Task<OperationResult<PortalUser>> HandleAsync(CreatePortalUserCommand command, CancellationToken cancellationToken);
}

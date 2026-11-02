using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Handlers;

internal sealed class CurrentUserHandler(IPortalUserRepository users) : ICurrentUserHandler
{
    public async Task<OperationResult<PortalUser>> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId, cancellationToken);

        return user is null
            ? OperationResult<PortalUser>.Failed(OperationOutcome.NotFound)
            : OperationResult<PortalUser>.Succeeded(user);
    }
}

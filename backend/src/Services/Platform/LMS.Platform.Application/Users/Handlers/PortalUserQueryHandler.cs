using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Application.Users.Models;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Handlers;

internal sealed class PortalUserQueryHandler(IPortalUserRepository users) : IPortalUserQueryHandler
{
    public async Task<OperationResult<PageResult<PortalUser>>> ListAsync(
        ListPortalUsersQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<PortalUser>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<PortalUser>>.Succeeded(await users.ListAsync(query, cancellationToken));
    }

    public async Task<OperationResult<PortalUser>> GetAsync(string userId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var user = await users.FindVisibleAsync(userId, scope, cancellationToken);

        return user is null
            ? OperationResult<PortalUser>.Failed(OperationOutcome.NotFound)
            : OperationResult<PortalUser>.Succeeded(user);
    }
}

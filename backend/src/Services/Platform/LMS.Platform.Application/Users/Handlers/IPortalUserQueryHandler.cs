using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Application.Users.Models;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Handlers;

public interface IPortalUserQueryHandler
{
    Task<OperationResult<PageResult<PortalUser>>> ListAsync(ListPortalUsersQuery query, CancellationToken cancellationToken);

    Task<OperationResult<PortalUser>> GetAsync(string userId, PropertyScope scope, CancellationToken cancellationToken);
}

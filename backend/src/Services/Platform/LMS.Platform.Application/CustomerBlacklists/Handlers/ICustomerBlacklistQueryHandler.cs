using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.CustomerBlacklists.Models;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Application.CustomerBlacklists.Handlers;

public interface ICustomerBlacklistQueryHandler
{
    Task<OperationResult<PageResult<CustomerBlacklist>>> ListAsync(ListCustomerBlacklistsQuery query, CancellationToken cancellationToken);

    Task<OperationResult<CustomerBlacklist>> GetAsync(string blacklistId, CancellationToken cancellationToken);
}

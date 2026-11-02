using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.CustomerBlacklists.Models;
using LMS.Platform.Application.CustomerBlacklists.Ports;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Application.CustomerBlacklists.Handlers;

internal sealed class CustomerBlacklistQueryHandler(ICustomerBlacklistRepository blacklists) : ICustomerBlacklistQueryHandler
{
    public async Task<OperationResult<PageResult<CustomerBlacklist>>> ListAsync(
        ListCustomerBlacklistsQuery query,
        CancellationToken cancellationToken) =>
        OperationResult<PageResult<CustomerBlacklist>>.Succeeded(await blacklists.ListAsync(query, cancellationToken));

    public async Task<OperationResult<CustomerBlacklist>> GetAsync(string blacklistId, CancellationToken cancellationToken)
    {
        var blacklist = await blacklists.FindByIdAsync(blacklistId, cancellationToken);

        return blacklist is null
            ? OperationResult<CustomerBlacklist>.Failed(OperationOutcome.NotFound)
            : OperationResult<CustomerBlacklist>.Succeeded(blacklist);
    }
}

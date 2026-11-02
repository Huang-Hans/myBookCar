using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.CustomerBlacklists.Commands;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Application.CustomerBlacklists.Handlers;

public interface ICreateCustomerBlacklistHandler
{
    Task<OperationResult<CustomerBlacklist>> HandleAsync(CreateCustomerBlacklistCommand command, CancellationToken cancellationToken);
}

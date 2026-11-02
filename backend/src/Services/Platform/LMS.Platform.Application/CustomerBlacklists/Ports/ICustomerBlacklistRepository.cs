using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.CustomerBlacklists.Models;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Application.CustomerBlacklists.Ports;

public interface ICustomerBlacklistRepository
{
    Task<CustomerBlacklist?> FindByIdAsync(string blacklistId, CancellationToken cancellationToken);

    Task<CustomerBlacklist?> FindAsync(string blacklistId, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<CustomerBlacklist>> ListAsync(ListCustomerBlacklistsQuery query, CancellationToken cancellationToken);

    // 当前有效的限制：启用，且 today 落在 [effectiveDate, expiryDate] 内（含边界日）。会员号不区分大小写匹配。
    Task<IReadOnlyList<CustomerBlacklist>> FindActiveByMemberIdsAsync(
        IReadOnlyList<string> memberIds,
        DateOnly today,
        CancellationToken cancellationToken);

    // 会员号（不区分大小写）重复时抛出 DuplicateRecordException。
    Task InsertAsync(CustomerBlacklist blacklist, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(CustomerBlacklist updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}

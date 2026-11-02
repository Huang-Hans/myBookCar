using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.CustomerBlacklists.Models;
using LMS.Platform.Application.CustomerBlacklists.Ports;
using LMS.Platform.Domain.CustomerBlacklists;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoCustomerBlacklistRepository(IMongoDatabase database) : ICustomerBlacklistRepository
{
    private static readonly string[] SearchFields = ["_id", "customerName", "memberId", "reason", "effectiveDate", "expiryDate"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["blacklistId"] = "_id",
        ["customerName"] = "customerName",
        ["memberId"] = "memberId",
        ["reason"] = "reason",
        ["effectiveDate"] = "effectiveDate",
        ["expiryDate"] = "expiryDate",
        ["isActive"] = "isActive"
    };

    private IMongoCollection<CustomerBlacklistDocument> Collection =>
        database.GetCollection<CustomerBlacklistDocument>(PlatformCollections.CustomerBlacklists);

    public async Task<CustomerBlacklist?> FindByIdAsync(string blacklistId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.BlacklistId == blacklistId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CustomerBlacklist?> FindAsync(string blacklistId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.BlacklistId == blacklistId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<CustomerBlacklist>> ListAsync(ListCustomerBlacklistsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<CustomerBlacklistDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<CustomerBlacklist>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<IReadOnlyList<CustomerBlacklist>> FindActiveByMemberIdsAsync(
        IReadOnlyList<string> memberIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var filter = Builders<CustomerBlacklistDocument>.Filter;
        var day = StoredValues.FromDate(today);
        var active = filter.And(
            filter.In(candidate => candidate.MemberId, memberIds),
            filter.Eq(candidate => candidate.IsActive, true),
            filter.Lte(candidate => candidate.EffectiveDate, day),
            filter.Gte(candidate => candidate.ExpiryDate, day));

        // 会员号不区分大小写：查询使用与唯一索引相同的排序规则。
        var documents = await Collection
            .Find(active, new FindOptions { Collation = new Collation("en", strength: CollationStrength.Secondary) })
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public Task InsertAsync(CustomerBlacklist blacklist, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), CustomerBlacklistDocument.FromDomain(blacklist), cancellationToken);

    public Task<bool> TryReplaceAsync(
        CustomerBlacklist updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.BlacklistId,
            expectedVersion,
            CustomerBlacklistDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    private static FilterDefinition<CustomerBlacklistDocument> BuildFilter(ListCustomerBlacklistsQuery query)
    {
        var filter = Builders<CustomerBlacklistDocument>.Filter;

        return new MongoFilterBuilder<CustomerBlacklistDocument>(FilterDefinition<CustomerBlacklistDocument>.Empty)
            .When(query.IsActive is not null, () => filter.Eq(candidate => candidate.IsActive, query.IsActive!.Value))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<CustomerBlacklistDocument>(SearchFields, query.List.Search))
            .Build();
    }
}

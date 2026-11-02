using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Overrides.Models;
using LMS.Booking.Application.Overrides.Ports;
using LMS.Booking.Domain.Overrides;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoQuotaOverrideRepository(IMongoDatabase database) : IQuotaOverrideRepository
{
    private static readonly string[] SearchFields = ["_id", "propertyId", "guestId", "justification", "status", "requestedBy"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["overrideId"] = "_id",
        ["propertyId"] = "propertyId",
        ["guestId"] = "guestId",
        ["additionalDailyEntitlements"] = "additionalDailyEntitlements",
        ["justification"] = "justification",
        ["status"] = "status",
        ["requestedBy"] = "requestedBy",
        ["requestedAt"] = "requestedAt"
    };

    private IMongoCollection<QuotaOverrideDocument> Collection =>
        database.GetCollection<QuotaOverrideDocument>(BookingCollections.QuotaOverrides);

    public async Task<QuotaOverride?> FindByIdAsync(string overrideId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.OverrideId == overrideId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<QuotaOverride?> FindVisibleAsync(
        string overrideId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var filter = Builders<QuotaOverrideDocument>.Filter.And(
            Builders<QuotaOverrideDocument>.Filter.Eq(candidate => candidate.OverrideId, overrideId), VisibleTo(scope));
        var document = await Collection.Find(session, filter).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<QuotaOverride>> ListAsync(ListQuotaOverridesQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<QuotaOverrideDocument>.Filter;
        var combined = new MongoFilterBuilder<QuotaOverrideDocument>(VisibleTo(query.Scope))
            .When(query.PropertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, query.PropertyId))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<QuotaOverrideDocument>(SearchFields, query.List.Search))
            .Build();
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, combined, MongoSortFactory.Create<QuotaOverrideDocument>(query.List.Sort, SortFields), query.List, cancellationToken);

        return new PageResult<QuotaOverride>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public Task InsertAsync(QuotaOverride quotaOverride, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), QuotaOverrideDocument.FromDomain(quotaOverride), cancellationToken);

    public Task<bool> TryReplaceAsync(
        QuotaOverride updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.OverrideId,
            expectedVersion,
            QuotaOverrideDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    private static FilterDefinition<QuotaOverrideDocument> VisibleTo(PropertyScope scope) =>
        scope.IsAllProperties
            ? FilterDefinition<QuotaOverrideDocument>.Empty
            : Builders<QuotaOverrideDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);
}

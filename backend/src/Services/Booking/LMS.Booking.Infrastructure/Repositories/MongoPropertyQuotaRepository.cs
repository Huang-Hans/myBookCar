using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Quotas.Models;
using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Domain.Quotas;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoPropertyQuotaRepository(IMongoDatabase database) : IPropertyQuotaRepository
{
    private static readonly string[] SearchFields = ["_id", "exhaustionPolicy"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["propertyId"] = "_id",
        ["dailyQuota"] = "dailyQuota",
        ["weeklyQuota"] = "weeklyQuota",
        ["monthlyQuota"] = "monthlyQuota",
        ["dailyUsed"] = "dailyUsed",
        ["weeklyUsed"] = "weeklyUsed",
        ["monthlyUsed"] = "monthlyUsed",
        ["exhaustionPolicy"] = "exhaustionPolicy"
    };

    private IMongoCollection<PropertyQuotaDocument> Collection =>
        database.GetCollection<PropertyQuotaDocument>(BookingCollections.PropertyQuotas);

    public async Task<PropertyQuota?> FindByIdAsync(string propertyId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.PropertyId == propertyId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PropertyQuota?> FindVisibleAsync(string propertyId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(propertyId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PropertyQuota?> FindVisibleAsync(
        string propertyId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdVisibleTo(propertyId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<PropertyQuota>> ListAsync(ListPropertyQuotasQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<PropertyQuotaDocument>.Filter;
        var combined = new MongoFilterBuilder<PropertyQuotaDocument>(VisibleTo(query.Scope))
            .When(query.PropertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, query.PropertyId))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<PropertyQuotaDocument>(SearchFields, query.List.Search))
            .Build();
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, combined, MongoSortFactory.Create<PropertyQuotaDocument>(query.List.Sort, SortFields), query.List, cancellationToken);

        return new PageResult<PropertyQuota>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<IReadOnlyList<PropertyQuota>> ListVisibleAsync(PropertyScope scope, CancellationToken cancellationToken)
    {
        var documents = await Collection.Find(VisibleTo(scope)).ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    // 只写入可编辑的限额和耗尽策略，不触碰由权威来源维护的已用量。
    public Task<bool> TryReplaceAsync(
        PropertyQuota updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var update = Builders<PropertyQuotaDocument>.Update
            .Set(candidate => candidate.DailyQuota, updated.DailyQuota)
            .Set(candidate => candidate.WeeklyQuota, updated.WeeklyQuota)
            .Set(candidate => candidate.MonthlyQuota, updated.MonthlyQuota)
            .Set(candidate => candidate.ExhaustionPolicy, PropertyQuotaDocument.PolicyToStored(updated.ExhaustionPolicy));

        return VersionedUpdate.TryApplyAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            Builders<PropertyQuotaDocument>.Filter.Eq(candidate => candidate.PropertyId, updated.PropertyId),
            expectedVersion,
            update,
            cancellationToken);
    }

    private static FilterDefinition<PropertyQuotaDocument> ByIdVisibleTo(string propertyId, PropertyScope scope) =>
        Builders<PropertyQuotaDocument>.Filter.And(
            Builders<PropertyQuotaDocument>.Filter.Eq(candidate => candidate.PropertyId, propertyId),
            VisibleTo(scope));

    private static FilterDefinition<PropertyQuotaDocument> VisibleTo(PropertyScope scope) =>
        scope.IsAllProperties
            ? FilterDefinition<PropertyQuotaDocument>.Empty
            : Builders<PropertyQuotaDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);
}

using System.Text.RegularExpressions;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Domain.Coupons;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoCouponRepository(IMongoDatabase database) : ICouponRepository
{
    private static readonly string[] SearchFields = ["code", "name"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["couponId"] = "_id",
        ["code"] = "code",
        ["name"] = "name",
        ["memberId"] = "memberId",
        ["type"] = "type",
        ["validFrom"] = "validFrom",
        ["validTo"] = "validTo",
        ["issuedQuantity"] = "issuedQuantity",
        ["redeemedCount"] = "redeemedCount"
    };

    private IMongoCollection<CouponDocument> Collection => database.GetCollection<CouponDocument>(DispatchCollections.Coupons);

    public async Task<Coupon?> FindByIdAsync(string couponId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.CouponId == couponId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Coupon?> FindByIdAsync(string couponId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.CouponId == couponId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Coupon?> FindVisibleAsync(string couponId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(couponId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Coupon?> FindVisibleAsync(string couponId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdVisibleTo(couponId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task<Coupon?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        FindByCodeAsync(Collection.Find(CodeFilter(code)), cancellationToken);

    public Task<Coupon?> FindByCodeAsync(string code, ITransactionContext context, CancellationToken cancellationToken) =>
        FindByCodeAsync(Collection.Find(MongoTransactionContext.RequireSession(context), CodeFilter(code)), cancellationToken);

    public async Task<PageResult<Coupon>> ListAsync(ListCouponsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<CouponDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<Coupon>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<CouponSummaryCounts> SummarizeAsync(PropertyScope scope, DateOnly today, CancellationToken cancellationToken)
    {
        var visible = VisibleFilter(scope);
        var day = StoredValues.FromDate(today);
        var active = await Collection.CountDocumentsAsync(Builders<CouponDocument>.Filter.And(visible, StatusFilter(CouponStatus.Active, day)), cancellationToken: cancellationToken);
        var upcoming = await Collection.CountDocumentsAsync(Builders<CouponDocument>.Filter.And(visible, StatusFilter(CouponStatus.Upcoming, day)), cancellationToken: cancellationToken);
        var totals = await Collection.Aggregate()
            .Match(visible)
            .Group(document => 1, group => new QuantityTotals(group.Sum(document => document.IssuedQuantity), group.Sum(document => document.RedeemedCount)))
            .FirstOrDefaultAsync(cancellationToken);

        return new CouponSummaryCounts(active, totals?.Issued ?? 0, totals?.Redeemed ?? 0, upcoming);
    }

    public Task InsertAsync(Coupon coupon, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(Collection, MongoTransactionContext.RequireSession(context), CouponDocument.FromDomain(coupon), cancellationToken);

    public Task<bool> TryReplaceAsync(Coupon updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.CouponId,
            expectedVersion,
            CouponDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    public async Task<bool> DeleteAsync(string couponId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var result = await Collection.DeleteOneAsync(
            MongoTransactionContext.RequireSession(context),
            candidate => candidate.CouponId == couponId,
            cancellationToken: cancellationToken);

        return result.DeletedCount == 1;
    }

    private static FilterDefinition<CouponDocument> CodeFilter(string code) =>
        Builders<CouponDocument>.Filter.Regex(candidate => candidate.Code, new BsonRegularExpression($"^{Regex.Escape(code)}$", "i"));

    private static async Task<Coupon?> FindByCodeAsync(IFindFluent<CouponDocument, CouponDocument> query, CancellationToken cancellationToken)
    {
        var document = await query.FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    private static FilterDefinition<CouponDocument> BuildFilter(ListCouponsQuery query)
    {
        var filter = Builders<CouponDocument>.Filter;
        var day = StoredValues.FromDate(query.Today);

        return new MongoFilterBuilder<CouponDocument>(VisibleFilter(query.Scope))
            .When(query.MemberId is not null, () => filter.Eq(candidate => candidate.MemberId, query.MemberId))
            .When(query.Type is not null, () => filter.Eq(candidate => candidate.Type, WireEnum.ToWire(query.Type!.Value)))
            .When(query.Status is not null, () => StatusFilter(query.Status!.Value, day))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<CouponDocument>(SearchFields, query.List.Search))
            .Build();
    }

    private static FilterDefinition<CouponDocument> VisibleFilter(PropertyScope scope)
    {
        if (scope.IsAllProperties)
        {
            return FilterDefinition<CouponDocument>.Empty;
        }

        var filter = Builders<CouponDocument>.Filter;
        var globalScopes = new[] { WireEnum.ToWire(CouponScope.AllProperties), WireEnum.ToWire(CouponScope.Vehicle) };

        return filter.Or(
            filter.Eq(candidate => candidate.PropertyId, scope.PropertyId),
            filter.In(candidate => candidate.Scope, globalScopes));
    }

    private static FilterDefinition<CouponDocument> StatusFilter(CouponStatus status, string today)
    {
        var filter = Builders<CouponDocument>.Filter;
        var enabled = filter.Eq(candidate => candidate.Disabled, false);
        var notExhausted = RedeemedCompared("$lt");
        var exhausted = RedeemedCompared("$gte");

        return status switch
        {
            CouponStatus.Disabled => filter.Eq(candidate => candidate.Disabled, true),
            CouponStatus.Exhausted => filter.And(enabled, exhausted),
            CouponStatus.Upcoming => filter.And(enabled, notExhausted, filter.Gt(candidate => candidate.ValidFrom, today)),
            CouponStatus.Expired => filter.And(enabled, notExhausted, filter.Lt(candidate => candidate.ValidTo, today)),
            _ => filter.And(enabled, notExhausted, filter.Lte(candidate => candidate.ValidFrom, today), filter.Gte(candidate => candidate.ValidTo, today))
        };
    }

    private static FilterDefinition<CouponDocument> RedeemedCompared(string comparison) =>
        new BsonDocumentFilterDefinition<CouponDocument>(
            new BsonDocument("$expr", new BsonDocument(comparison, new BsonArray { "$redeemedCount", "$issuedQuantity" })));

    private static FilterDefinition<CouponDocument> ByIdVisibleTo(string couponId, PropertyScope scope) =>
        Builders<CouponDocument>.Filter.And(
            Builders<CouponDocument>.Filter.Eq(candidate => candidate.CouponId, couponId),
            VisibleFilter(scope));

    private sealed record QuantityTotals(int Issued, int Redeemed);
}

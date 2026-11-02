using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Domain.Approvals;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoApprovalRepository(IMongoDatabase database) : IApprovalRepository
{
    private static readonly string[] SearchFields =
    [
        "_id", "tripReference", "guest.name", "guest.guestId", "guest.tierCode", "propertyId", "route.pickupName", "route.dropoffName", "status"
    ];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["approvalId"] = "_id",
        ["tripReference"] = "tripReference",
        ["guest.name"] = "guest.name",
        ["guest.tierCode"] = "guest.tierCode",
        ["propertyId"] = "propertyId",
        ["route.pickupName"] = "route.pickupName",
        ["route.dropoffName"] = "route.dropoffName",
        ["pickupAt"] = "pickupAt",
        ["slaDeadlineAt"] = "slaDeadlineAt",
        ["status"] = "status"
    };

    private IMongoCollection<CrossBorderApprovalDocument> Collection =>
        database.GetCollection<CrossBorderApprovalDocument>(BookingCollections.CrossBorderApprovals);

    public async Task<CrossBorderApproval?> FindVisibleAsync(string approvalId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(approvalId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CrossBorderApproval?> FindVisibleAsync(
        string approvalId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdVisibleTo(approvalId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<CrossBorderApproval?> FindByIdAsync(string approvalId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.ApprovalId == approvalId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<CrossBorderApproval>> FindVisibleByIdsAsync(
        IReadOnlyList<string> approvalIds,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var documents = await Collection.Find(ByIdsVisibleTo(approvalIds, scope)).ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<CrossBorderApproval>> FindVisibleByIdsAsync(
        IReadOnlyList<string> approvalIds,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var documents = await Collection.Find(session, ByIdsVisibleTo(approvalIds, scope)).ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<CrossBorderApproval>> FindByIdsAsync(IReadOnlyList<string> approvalIds, CancellationToken cancellationToken)
    {
        var documents = await Collection.Find(Builders<CrossBorderApprovalDocument>.Filter.In(candidate => candidate.ApprovalId, approvalIds))
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<PageResult<CrossBorderApproval>> ListAsync(ListApprovalsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<CrossBorderApprovalDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<CrossBorderApproval>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<ApprovalSummary> SummarizeAsync(ListApprovalsQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<CrossBorderApprovalDocument>.Filter;
        var pending = filter.And(BuildFilter(query), filter.Eq(candidate => candidate.Status, WireEnum.ToWire(ApprovalStatus.Pending)));
        var count = await Collection.CountDocumentsAsync(pending, cancellationToken: cancellationToken);
        var earliest = await Collection.Find(pending).SortBy(candidate => candidate.PickupAt).ThenBy(candidate => candidate.ApprovalId)
            .Limit(1).FirstOrDefaultAsync(cancellationToken);

        return new ApprovalSummary(checked((int)count), earliest?.ApprovalChain ?? []);
    }

    public Task<bool> TryReplaceAsync(
        CrossBorderApproval updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.ApprovalId,
            expectedVersion,
            CrossBorderApprovalDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    private static FilterDefinition<CrossBorderApprovalDocument> BuildFilter(ListApprovalsQuery query)
    {
        var filter = Builders<CrossBorderApprovalDocument>.Filter;

        return new MongoFilterBuilder<CrossBorderApprovalDocument>(VisibleTo(query.Scope))
            .When(query.PropertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, query.PropertyId))
            .When(query.Status is not null, () => filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .When(query.From is not null, () => filter.Gte(candidate => candidate.PickupAt, UtcDateTimes.ToStored(query.From!.Value)))
            .When(query.To is not null, () => filter.Lt(candidate => candidate.PickupAt, UtcDateTimes.ToStored(query.To!.Value)))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<CrossBorderApprovalDocument>(SearchFields, query.List.Search))
            .Build();
    }

    private static FilterDefinition<CrossBorderApprovalDocument> ByIdVisibleTo(string approvalId, PropertyScope scope) =>
        Builders<CrossBorderApprovalDocument>.Filter.And(
            Builders<CrossBorderApprovalDocument>.Filter.Eq(candidate => candidate.ApprovalId, approvalId),
            VisibleTo(scope));

    private static FilterDefinition<CrossBorderApprovalDocument> ByIdsVisibleTo(IReadOnlyList<string> approvalIds, PropertyScope scope) =>
        Builders<CrossBorderApprovalDocument>.Filter.And(
            Builders<CrossBorderApprovalDocument>.Filter.In(candidate => candidate.ApprovalId, approvalIds),
            VisibleTo(scope));

    private static FilterDefinition<CrossBorderApprovalDocument> VisibleTo(PropertyScope scope) =>
        scope.IsAllProperties
            ? FilterDefinition<CrossBorderApprovalDocument>.Empty
            : Builders<CrossBorderApprovalDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);
}

using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Application.Qualifications.Ports;
using LMS.Booking.Domain.Qualifications;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoQualificationRepository(IMongoDatabase database) : IQualificationRepository
{
    private static readonly string[] SearchFields =
    [
        "_id", "guest.name", "guest.guestId", "guest.tierCode", "propertyId", "loyaltyOutcome", "qualificationReference",
        "entitlementType", "status"
    ];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["qualificationId"] = "_id",
        ["guest.name"] = "guest.name",
        ["guest.guestId"] = "guest.guestId",
        ["guest.tierCode"] = "guest.tierCode",
        ["propertyId"] = "propertyId",
        ["loyaltyOutcome"] = "loyaltyOutcome",
        ["qualificationReference"] = "qualificationReference",
        ["status"] = "status"
    };

    private IMongoCollection<QualificationReferenceDocument> Collection =>
        database.GetCollection<QualificationReferenceDocument>(BookingCollections.QualificationReferences);

    public async Task<QualificationReference?> FindByIdAsync(string qualificationId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.QualificationId == qualificationId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<QualificationReference?> FindVisibleAsync(string qualificationId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(qualificationId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<QualificationReference?> FindVisibleAsync(
        string qualificationId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdVisibleTo(qualificationId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<QualificationReference>> ListAsync(ListQualificationsQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<QualificationReferenceDocument>.Filter;
        var combined = new MongoFilterBuilder<QualificationReferenceDocument>(VisibleTo(query.Scope))
            .When(query.PropertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, query.PropertyId))
            .When(query.Status is not null, () => filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<QualificationReferenceDocument>(SearchFields, query.List.Search))
            .Build();
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, combined, MongoSortFactory.Create<QualificationReferenceDocument>(query.List.Sort, SortFields), query.List, cancellationToken);

        return new PageResult<QualificationReference>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<QualificationReference?> FindPendingByGuestAsync(string guestId, string propertyId, CancellationToken cancellationToken)
    {
        var filter = Builders<QualificationReferenceDocument>.Filter.And(
            Builders<QualificationReferenceDocument>.Filter.Eq("guest.guestId", guestId),
            Builders<QualificationReferenceDocument>.Filter.Eq(candidate => candidate.PropertyId, propertyId),
            Builders<QualificationReferenceDocument>.Filter.Eq(
                candidate => candidate.Status, WireEnum.ToWire(QualificationStatus.ManagerApprovalRequired)));
        var document = await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<QualificationReference?> FindOpenRequestByGuestAsync(string guestId, string propertyId, CancellationToken cancellationToken)
    {
        var filter = Builders<QualificationReferenceDocument>.Filter.And(
            Builders<QualificationReferenceDocument>.Filter.Eq("guest.guestId", guestId),
            Builders<QualificationReferenceDocument>.Filter.Eq(candidate => candidate.PropertyId, propertyId),
            Builders<QualificationReferenceDocument>.Filter.In(
                candidate => candidate.Status,
                [WireEnum.ToWire(QualificationStatus.Escalated), WireEnum.ToWire(QualificationStatus.ManagerApprovalRequired)]));
        var documents = await Collection.Find(filter).ToListAsync(cancellationToken);
        var open = documents.Select(document => document.ToDomain()).ToList();

        return open.FirstOrDefault(item => item.Status == QualificationStatus.Escalated) ?? open.FirstOrDefault();
    }

    public Task InsertAsync(QualificationReference reference, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), QualificationReferenceDocument.FromDomain(reference), cancellationToken);

    public Task<bool> TryReplaceAsync(
        QualificationReference updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.QualificationId,
            expectedVersion,
            QualificationReferenceDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    private static FilterDefinition<QualificationReferenceDocument> ByIdVisibleTo(string qualificationId, PropertyScope scope) =>
        Builders<QualificationReferenceDocument>.Filter.And(
            Builders<QualificationReferenceDocument>.Filter.Eq(candidate => candidate.QualificationId, qualificationId),
            VisibleTo(scope));

    private static FilterDefinition<QualificationReferenceDocument> VisibleTo(PropertyScope scope) =>
        scope.IsAllProperties
            ? FilterDefinition<QualificationReferenceDocument>.Empty
            : Builders<QualificationReferenceDocument>.Filter.Eq(candidate => candidate.PropertyId, scope.PropertyId);
}

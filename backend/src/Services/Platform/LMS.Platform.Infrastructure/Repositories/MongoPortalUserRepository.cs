using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Users.Models;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Domain.Users;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoPortalUserRepository(IMongoDatabase database) : IPortalUserRepository
{
    private static readonly string[] SearchFields = ["_id", "name", "corporateEmail", "roleCode", "propertyId", "identityProvider"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["userId"] = "_id",
        ["name"] = "name",
        ["corporateEmail"] = "corporateEmail",
        ["roleCode"] = "roleCode",
        ["propertyId"] = "propertyId",
        ["identityProvider"] = "identityProvider",
        ["isActive"] = "isActive"
    };

    private IMongoCollection<PortalUserDocument> Collection =>
        database.GetCollection<PortalUserDocument>(PlatformCollections.PortalUsers);

    public async Task<PortalUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.UserId == userId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PortalUser?> FindVisibleAsync(string userId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(ByIdVisibleTo(userId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PortalUser?> FindVisibleAsync(
        string userId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, ByIdVisibleTo(userId, scope)).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PortalUser?> FindBySubjectAsync(string identityProvider, string subjectId, CancellationToken cancellationToken)
    {
        var document = await Collection
            .Find(candidate => candidate.IdentityProvider == identityProvider && candidate.SubjectId == subjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PortalUser?> FindUnlinkedByEmailAsync(
        string identityProvider,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PortalUserDocument>.Filter;
        var document = await Collection
            .Find(filter.And(
                filter.Eq(candidate => candidate.IdentityProvider, identityProvider),
                filter.Eq(candidate => candidate.CorporateEmail, normalizedEmail),
                filter.Eq(candidate => candidate.SubjectId, null)))
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<PortalUser>> ListNotificationCandidatesAsync(
        string propertyId,
        IReadOnlyCollection<string> roleCodes,
        int take,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PortalUserDocument>.Filter;
        var documents = await Collection
            .Find(filter.And(
                filter.Eq(candidate => candidate.IsActive, true),
                filter.Ne(candidate => candidate.SubjectId, null),
                filter.In(candidate => candidate.RoleCode, roleCodes),
                filter.Or(
                    filter.Eq(candidate => candidate.HasAllPropertyAccess, true),
                    filter.Eq(candidate => candidate.PropertyId, propertyId))))
            .Limit(take)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<PageResult<PortalUser>> ListAsync(ListPortalUsersQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<PortalUserDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<PortalUser>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public Task InsertAsync(PortalUser user, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), PortalUserDocument.FromDomain(user), cancellationToken);

    public Task<bool> TryReplaceAsync(
        PortalUser updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.UserId,
            expectedVersion,
            PortalUserDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    private static FilterDefinition<PortalUserDocument> ByIdVisibleTo(string userId, PropertyScope scope) =>
        Builders<PortalUserDocument>.Filter.And(
            Builders<PortalUserDocument>.Filter.Eq(candidate => candidate.UserId, userId),
            VisibleTo(scope));

    // 非全物业调用者只能看到属于自己物业的单一物业用户；全物业用户由全物业调用者管理。
    private static FilterDefinition<PortalUserDocument> VisibleTo(PropertyScope scope)
    {
        var filter = Builders<PortalUserDocument>.Filter;

        return scope.IsAllProperties
            ? FilterDefinition<PortalUserDocument>.Empty
            : filter.And(filter.Eq(candidate => candidate.HasAllPropertyAccess, false), filter.Eq(candidate => candidate.PropertyId, scope.PropertyId));
    }

    private static FilterDefinition<PortalUserDocument> BuildFilter(ListPortalUsersQuery query)
    {
        var filter = Builders<PortalUserDocument>.Filter;

        return new MongoFilterBuilder<PortalUserDocument>(VisibleTo(query.Scope))
            .When(query.PropertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, query.PropertyId))
            .When(query.IsActive is not null, () => filter.Eq(candidate => candidate.IsActive, query.IsActive!.Value))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<PortalUserDocument>(SearchFields, query.List.Search))
            .Build();
    }
}

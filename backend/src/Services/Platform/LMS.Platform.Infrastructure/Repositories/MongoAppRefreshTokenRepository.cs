using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Authentication.Ports;
using LMS.Platform.Domain.Authentication;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoAppRefreshTokenRepository(IMongoDatabase database) : IAppRefreshTokenRepository
{
    private IMongoCollection<AppRefreshTokenDocument> Collection =>
        database.GetCollection<AppRefreshTokenDocument>(PlatformCollections.AppRefreshTokens);

    public async Task<AppRefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.TokenHash == tokenHash).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(AppRefreshToken token, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), AppRefreshTokenDocument.FromDomain(token), cancellationToken);

    public Task<bool> TryReplaceAsync(
        AppRefreshToken updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken) =>
        MongoReplace.VersionedAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            updated.TokenId,
            expectedVersion,
            AppRefreshTokenDocument.FromDomain(updated with { Version = expectedVersion + 1 }),
            cancellationToken);

    public Task RevokeFamilyAsync(string familyId, DateTimeOffset revokedAt, ITransactionContext context, CancellationToken cancellationToken)
    {
        var filter = Builders<AppRefreshTokenDocument>.Filter.And(
            Builders<AppRefreshTokenDocument>.Filter.Eq(AppRefreshTokenDocument.FieldNames.FamilyId, familyId),
            Builders<AppRefreshTokenDocument>.Filter.Eq(AppRefreshTokenDocument.FieldNames.RevokedAt, (DateTime?)null));
        var update = Builders<AppRefreshTokenDocument>.Update
            .Set(AppRefreshTokenDocument.FieldNames.RevokedAt, UtcDateTimes.ToStored(revokedAt))
            .Inc(VersionedUpdate.VersionField, 1L);

        return Collection.UpdateManyAsync(MongoTransactionContext.RequireSession(context), filter, update, cancellationToken: cancellationToken);
    }
}

using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Domain.Coupons;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoCouponRedemptionRepository(IMongoDatabase database) : ICouponRedemptionRepository
{
    private IMongoCollection<CouponRedemptionDocument> Collection =>
        database.GetCollection<CouponRedemptionDocument>(DispatchCollections.CouponRedemptions);

    public Task<CouponRedemption?> FindAsync(string redemptionKey, CancellationToken cancellationToken) =>
        FindAsync(Collection.Find(candidate => candidate.RedemptionKey == redemptionKey), cancellationToken);

    public Task<CouponRedemption?> FindAsync(string redemptionKey, ITransactionContext context, CancellationToken cancellationToken) =>
        FindAsync(
            Collection.Find(MongoTransactionContext.RequireSession(context), candidate => candidate.RedemptionKey == redemptionKey),
            cancellationToken);

    public Task InsertAsync(CouponRedemption redemption, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            CouponRedemptionDocument.FromDomain(redemption),
            cancellationToken);

    public async Task<bool> TrySetRestoredAsync(string redemptionKey, bool restored, ITransactionContext context, CancellationToken cancellationToken)
    {
        var result = await Collection.UpdateOneAsync(
            MongoTransactionContext.RequireSession(context),
            candidate => candidate.RedemptionKey == redemptionKey && candidate.Restored != restored,
            Builders<CouponRedemptionDocument>.Update.Set(candidate => candidate.Restored, restored),
            cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    private static async Task<CouponRedemption?> FindAsync(
        IFindFluent<CouponRedemptionDocument, CouponRedemptionDocument> query,
        CancellationToken cancellationToken)
    {
        var document = await query.FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }
}

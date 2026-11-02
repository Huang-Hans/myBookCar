using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Staff.Ports;
using LMS.Booking.Domain.Staff;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoStaffPreferencesRepository(IMongoDatabase database) : IStaffPreferencesRepository
{
    private IMongoCollection<StaffPreferencesDocument> Collection =>
        database.GetCollection<StaffPreferencesDocument>(BookingCollections.StaffPreferences);

    public async Task<StaffPreferences?> FindAsync(string userId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.UserId == userId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task UpsertAsync(StaffPreferences preferences, CancellationToken cancellationToken) =>
        UpsertCoreAsync(preferences, null, cancellationToken);

    public Task UpsertAsync(StaffPreferences preferences, ITransactionContext context, CancellationToken cancellationToken) =>
        UpsertCoreAsync(preferences, MongoTransactionContext.RequireSession(context), cancellationToken);

    // 整体替换，不存在则插入；无版本控制（本人自助、低风险数据）。
    private async Task UpsertCoreAsync(StaffPreferences preferences, IClientSessionHandle? session, CancellationToken cancellationToken)
    {
        var document = StaffPreferencesDocument.FromDomain(preferences);
        var options = new ReplaceOptions { IsUpsert = true };

        if (session is null)
        {
            await Collection.ReplaceOneAsync(candidate => candidate.UserId == preferences.UserId, document, options, cancellationToken);
        }
        else
        {
            await Collection.ReplaceOneAsync(session, candidate => candidate.UserId == preferences.UserId, document, options, cancellationToken);
        }
    }
}

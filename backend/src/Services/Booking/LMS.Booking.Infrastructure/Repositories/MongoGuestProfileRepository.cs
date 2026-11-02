using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoGuestProfileRepository(IMongoDatabase database) : IGuestProfileRepository
{
    private IMongoCollection<GuestDocument> Collection =>
        database.GetCollection<GuestDocument>(BookingCollections.Guests);

    public async Task<GuestProfile?> FindAsync(string guestId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(MongoTransactionContext.RequireSession(context), item => item.GuestId == guestId)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<bool> UpdateAsync(GuestProfile guest, ITransactionContext context, CancellationToken cancellationToken)
    {
        var update = Builders<GuestDocument>.Update
            .Set(item => item.Name, guest.Name)
            .Set(item => item.TierCode, guest.TierCode)
            .Set(item => item.RoomNumber, guest.RoomNumber)
            .Set(item => item.PropertyId, guest.PropertyId);
        var result = await Collection.UpdateOneAsync(
            MongoTransactionContext.RequireSession(context), item => item.GuestId == guest.GuestId,
            update, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }
}

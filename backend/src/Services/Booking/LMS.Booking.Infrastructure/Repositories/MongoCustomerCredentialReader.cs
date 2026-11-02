using LMS.Booking.Application.Credentials.Models;
using LMS.Booking.Application.Credentials.Ports;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 只读取登录核对需要的两个字段。手机号与库内 phone 精确相等（普通索引 ix_phone），最多取两条：零条或多条都不登录，不选取第一条。
internal sealed class MongoCustomerCredentialReader(IMongoDatabase database) : ICustomerCredentialReader
{
    private IMongoCollection<GuestDocument> Collection => database.GetCollection<GuestDocument>(BookingCollections.Guests);

    public async Task<CustomerCredential?> FindByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        var matches = await Collection
            .Find(candidate => candidate.Phone == phone)
            .Limit(2)
            .Project(candidate => new CustomerCredential(candidate.GuestId, candidate.Password))
            .ToListAsync(cancellationToken);

        return matches.Count == 1 ? matches[0] : null;
    }

    public Task<CustomerCredential?> FindByIdAsync(string guestId, CancellationToken cancellationToken) =>
        Collection
            .Find(candidate => candidate.GuestId == guestId)
            .Project(candidate => new CustomerCredential(candidate.GuestId, candidate.Password))
            .FirstOrDefaultAsync(cancellationToken)!;
}

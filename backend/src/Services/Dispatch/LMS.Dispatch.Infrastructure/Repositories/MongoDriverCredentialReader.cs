using System.Linq.Expressions;
using LMS.Dispatch.Application.Credentials.Models;
using LMS.Dispatch.Application.Credentials.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

// 只读取登录核对需要的三个字段，不把整条司机记录装入内存。手机号与库内 phone 精确相等（普通索引 ix_phone），
// 最多取两条：零条或多条都不登录，不选取第一条。
internal sealed class MongoDriverCredentialReader(IMongoDatabase database) : IDriverCredentialReader
{
    private IMongoCollection<DriverDocument> Collection => database.GetCollection<DriverDocument>(DispatchCollections.Drivers);

    public async Task<DriverCredential?> FindByPhoneAsync(string phone, CancellationToken cancellationToken)
    {
        var matches = await Collection
            .Find(candidate => candidate.Phone == phone && candidate.DeletedAt == null)
            .Limit(2)
            .Project(Projection)
            .ToListAsync(cancellationToken);

        return matches.Count == 1 ? ToCredential(matches[0]) : null;
    }

    public async Task<DriverCredential?> FindByIdAsync(string driverId, CancellationToken cancellationToken)
    {
        var match = await Collection
            .Find(candidate => candidate.DriverId == driverId && candidate.DeletedAt == null)
            .Project(Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return match is null ? null : ToCredential(match);
    }

    private static Expression<Func<DriverDocument, StoredCredential>> Projection =>
        document => new StoredCredential(document.DriverId, document.Password, document.Status);

    private static DriverCredential ToCredential(StoredCredential stored) =>
        new(stored.DriverId, stored.Password, StoredValues.ParseEnum<DriverStatus>(stored.Status, "status"));

    private sealed record StoredCredential(string DriverId, string? Password, string Status);
}

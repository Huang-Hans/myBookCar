using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

// 租约锁：锁不存在或已过期时以 upsert 占用；他人仍持有时条件不成立，upsert 尝试插入同一 _id 触发主键冲突，即占用失败。
internal sealed class MongoReportRefreshLock(IMongoDatabase database, TimeProvider timeProvider) : IReportRefreshLock
{
    private static readonly FilterDefinitionBuilder<ReportRefreshLockDocument> Filter = Builders<ReportRefreshLockDocument>.Filter;

    private IMongoCollection<ReportRefreshLockDocument> Collection =>
        database.GetCollection<ReportRefreshLockDocument>(ReportingCollections.ReportRefreshLocks);

    public async Task<bool> TryAcquireAsync(string key, string holder, TimeSpan lease, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var filter = Filter.And(
            Filter.Eq(document => document.Key, key),
            Filter.Or(Filter.Lte(document => document.ExpiresAt, UtcDateTimes.ToStored(now)), Filter.Eq(document => document.Holder, holder)));
        var update = Builders<ReportRefreshLockDocument>.Update
            .Set(document => document.Holder, holder)
            .Set(document => document.ExpiresAt, UtcDateTimes.ToStored(now + lease));
        try
        {
            await Collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public Task ReleaseAsync(string key, string holder, CancellationToken cancellationToken) =>
        Collection.DeleteOneAsync(Filter.And(Filter.Eq(document => document.Key, key), Filter.Eq(document => document.Holder, holder)), cancellationToken);
}

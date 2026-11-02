using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Auditing.Ports;
using LMS.Platform.Domain.Auditing;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

// 只追加：审计记录写入后不提供更新和删除入口。
internal sealed class MongoAuditRecordRepository(IMongoDatabase database) : IAuditRecordRepository
{
    private IMongoCollection<AuditRecordDocument> Collection =>
        database.GetCollection<AuditRecordDocument>(PlatformCollections.AuditRecords);

    public Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            AuditRecordDocument.FromDomain(record),
            cancellationToken);
}

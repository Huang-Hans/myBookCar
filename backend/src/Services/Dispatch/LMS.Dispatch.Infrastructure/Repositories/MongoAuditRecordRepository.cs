using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Domain.Auditing;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

// 只追加：审计记录写入后不提供更新和删除入口。
internal sealed class MongoAuditRecordRepository(IMongoDatabase database) : IAuditRecordRepository
{
    private IMongoCollection<AuditRecordDocument> Collection =>
        database.GetCollection<AuditRecordDocument>(DispatchCollections.AuditRecords);

    public Task InsertManyAsync(IReadOnlyList<AuditRecord> records, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.ManyAsync(Collection, MongoTransactionContext.RequireSession(context),
            records.Select(AuditRecordDocument.FromDomain).ToList(), cancellationToken);

    public Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            AuditRecordDocument.FromDomain(record),
            cancellationToken);
}

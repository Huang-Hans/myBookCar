using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Auditing.Ports;
using LMS.Reporting.Domain.Auditing;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using LMS.Reporting.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

// 只追加：审计记录写入后不提供更新和删除入口。
internal sealed class MongoAuditRecordRepository(IMongoDatabase database) : IAuditRecordRepository
{
    private IMongoCollection<AuditRecordDocument> Collection =>
        database.GetCollection<AuditRecordDocument>(ReportingCollections.AuditRecords);

    public Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            AuditRecordDocument.FromDomain(record),
            cancellationToken);
}

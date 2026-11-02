using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 只追加：审计记录写入后不提供更新和删除入口。
internal sealed class MongoAuditRecordRepository(IMongoDatabase database) : IAuditRecordRepository
{
    private IMongoCollection<AuditRecordDocument> Collection =>
        database.GetCollection<AuditRecordDocument>(BookingCollections.AuditRecords);

    public Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            AuditRecordDocument.FromDomain(record),
            cancellationToken);
}

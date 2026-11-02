using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Idempotency.Models;
using LMS.Reporting.Application.Idempotency.Ports;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using LMS.Reporting.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

internal sealed class MongoIdempotencyRecordRepository(IMongoDatabase database) : IIdempotencyRecordRepository
{
    private IMongoCollection<IdempotencyRecordDocument> Collection =>
        database.GetCollection<IdempotencyRecordDocument>(ReportingCollections.IdempotencyRecords);

    public async Task<IdempotencyRecord?> FindActiveAsync(
        IdempotencyFingerprint fingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var id = IdempotencyRecordDocument.ComposeId(fingerprint);
        var nowStored = UtcDateTimes.ToStored(now);
        var document = await Collection
            .Find(candidate => candidate.Id == id && candidate.ExpiresAt > nowStored)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    // TTL 清理是异步的，已逻辑过期但尚未被清理的同键记录会占住唯一键，所以插入前先在事务内删除它。
    public async Task InsertAsync(IdempotencyRecord record, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var id = IdempotencyRecordDocument.ComposeId(record.Fingerprint);
        var createdAtStored = UtcDateTimes.ToStored(record.CreatedAt);

        await Collection.DeleteOneAsync(
            session,
            candidate => candidate.Id == id && candidate.ExpiresAt <= createdAtStored,
            cancellationToken: cancellationToken);
        await MongoInsert.OneAsync(Collection, session, IdempotencyRecordDocument.FromDomain(record), cancellationToken);
    }
}

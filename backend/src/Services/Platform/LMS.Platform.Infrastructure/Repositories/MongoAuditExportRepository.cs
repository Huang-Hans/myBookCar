using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Exports.Ports;
using LMS.Platform.Domain.Exports;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using LMS.Platform.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoAuditExportRepository(IMongoDatabase database) : IAuditExportRepository
{
    private static readonly string Queued = WireEnum.ToWire(ExportState.Queued);
    private static readonly string Running = WireEnum.ToWire(ExportState.Running);
    private static readonly string Succeeded = WireEnum.ToWire(ExportState.Succeeded);
    private static readonly string Failed = WireEnum.ToWire(ExportState.Failed);

    private static readonly FilterDefinitionBuilder<AuditExportDocument> Filters = Builders<AuditExportDocument>.Filter;
    private static readonly UpdateDefinitionBuilder<AuditExportDocument> Updates = Builders<AuditExportDocument>.Update;

    private IMongoCollection<AuditExportDocument> Collection =>
        database.GetCollection<AuditExportDocument>(PlatformCollections.ReportExports);

    public async Task<AuditExport?> FindAsync(string exportId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.ExportId == exportId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(AuditExport export, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), AuditExportDocument.FromDomain(export), cancellationToken);

    // 单次原子操作完成领取：多个实例同时轮询时，每个作业只会被一个实例领到。
    public async Task<AuditExport?> ClaimNextAsync(DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken)
    {
        var nowStored = UtcDateTimes.ToStored(now);
        var claimable = Filters.Or(
            Filters.Eq(candidate => candidate.State, Queued),
            Filters.And(Filters.Eq(candidate => candidate.State, Running), Filters.Lt(candidate => candidate.LeaseExpiresAt, nowStored)));
        var update = Updates
            .Set(candidate => candidate.State, Running)
            .Set(candidate => candidate.LeaseExpiresAt, UtcDateTimes.ToStored(leaseExpiresAt))
            .Inc(candidate => candidate.Version, 1);

        var document = await Collection.FindOneAndUpdateAsync(
            claimable,
            update,
            new FindOneAndUpdateOptions<AuditExportDocument>
            {
                Sort = Builders<AuditExportDocument>.Sort.Ascending(candidate => candidate.RequestedAt),
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);

        return document?.ToDomain();
    }

    public async Task<bool> CompleteAsync(
        string exportId,
        string resultFileId,
        DateTimeOffset expiresAt,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var result = await Collection.UpdateOneAsync(
            MongoTransactionContext.RequireSession(context),
            Filters.And(Filters.Eq(candidate => candidate.ExportId, exportId), Filters.Eq(candidate => candidate.State, Running)),
            Updates
                .Set(candidate => candidate.State, Succeeded)
                .Set(candidate => candidate.ResultFileId, resultFileId)
                .Set(candidate => candidate.ExpiresAt, UtcDateTimes.ToStored(expiresAt))
                .Unset(candidate => candidate.LeaseExpiresAt)
                .Inc(candidate => candidate.Version, 1),
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<bool> FailAsync(string exportId, string failureMessage, CancellationToken cancellationToken)
    {
        var result = await Collection.UpdateOneAsync(
            Filters.And(Filters.Eq(candidate => candidate.ExportId, exportId), Filters.Eq(candidate => candidate.State, Running)),
            Updates
                .Set(candidate => candidate.State, Failed)
                .Set(candidate => candidate.FailureMessage, failureMessage)
                .Unset(candidate => candidate.LeaseExpiresAt)
                .Inc(candidate => candidate.Version, 1),
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }
}

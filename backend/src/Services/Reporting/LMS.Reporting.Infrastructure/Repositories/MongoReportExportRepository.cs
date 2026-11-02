using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Exports.Ports;
using LMS.Reporting.Domain.Exports;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using LMS.Reporting.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

internal sealed class MongoReportExportRepository(IMongoDatabase database) : IReportExportRepository
{
    private static readonly string Queued = WireEnum.ToWire(ExportState.Queued);
    private static readonly string Running = WireEnum.ToWire(ExportState.Running);
    private static readonly string Succeeded = WireEnum.ToWire(ExportState.Succeeded);
    private static readonly string Failed = WireEnum.ToWire(ExportState.Failed);

    private static readonly FilterDefinitionBuilder<ReportExportDocument> Filters = Builders<ReportExportDocument>.Filter;
    private static readonly UpdateDefinitionBuilder<ReportExportDocument> Updates = Builders<ReportExportDocument>.Update;

    private IMongoCollection<ReportExportDocument> Collection =>
        database.GetCollection<ReportExportDocument>(ReportingCollections.ReportExports);

    public async Task<ReportExport?> FindAsync(string exportId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.ExportId == exportId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public Task InsertAsync(ReportExport export, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), ReportExportDocument.FromDomain(export), cancellationToken);

    // 单次原子操作完成领取：多个实例同时轮询时，每个作业只会被一个实例领到。
    public async Task<ReportExport?> ClaimNextAsync(DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken)
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
            new FindOneAndUpdateOptions<ReportExportDocument>
            {
                Sort = Builders<ReportExportDocument>.Sort.Ascending(candidate => candidate.RequestedAt),
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

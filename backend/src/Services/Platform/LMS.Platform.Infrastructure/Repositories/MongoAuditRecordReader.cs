using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Platform.Application.AuditRecords.Models;
using LMS.Platform.Application.AuditRecords.Ports;
using LMS.Platform.Domain.Auditing;
using LMS.Platform.Infrastructure.Documents;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Platform.Infrastructure.Repositories;

internal sealed class MongoAuditRecordReader(IMongoDatabase database) : IAuditRecordReader
{
    private static readonly string[] SearchFields =
        ["actor", "operation", "resourceType", "resourceId", "propertyId", "device", "sourceIp", "sessionReference"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["auditId"] = "_id",
        ["occurredAt"] = "occurredAt",
        ["actor"] = "actor",
        ["operation"] = "operation",
        ["resourceType"] = "resourceType",
        ["propertyId"] = "propertyId",
        ["device"] = "device",
        ["sourceIp"] = "sourceIp",
        ["sessionReference"] = "sessionReference",
        ["outcome"] = "outcome"
    };

    private IMongoCollection<AuditRecordDocument> Collection =>
        database.GetCollection<AuditRecordDocument>(PlatformCollections.AuditRecords);

    public async Task<PageResult<AuditRecord>> ListAsync(ListAuditRecordsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<AuditRecordDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<AuditRecord>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    private static FilterDefinition<AuditRecordDocument> BuildFilter(ListAuditRecordsQuery query)
    {
        var filter = Builders<AuditRecordDocument>.Filter;
        var propertyId = EffectivePropertyId(query);

        return new MongoFilterBuilder<AuditRecordDocument>(FilterDefinition<AuditRecordDocument>.Empty)
            .When(propertyId is not null, () => filter.Eq(candidate => candidate.PropertyId, propertyId))
            .When(query.From is not null, () => filter.Gte(candidate => candidate.OccurredAt, UtcDateTimes.ToStored(query.From!.Value)))
            .When(query.To is not null, () => filter.Lt(candidate => candidate.OccurredAt, UtcDateTimes.ToStored(query.To!.Value)))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<AuditRecordDocument>(SearchFields, query.List.Search))
            .Build();
    }

    // 非全物业调用者只看到自己物业的记录；显式物业筛选已由用例确认在授权范围内。
    private static string? EffectivePropertyId(ListAuditRecordsQuery query) =>
        query.PropertyId ?? (query.Scope.IsAllProperties ? null : query.Scope.PropertyId);
}

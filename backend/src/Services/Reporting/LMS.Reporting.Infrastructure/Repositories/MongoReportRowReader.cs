using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Reporting.Application.Reports.Models;
using LMS.Reporting.Application.Reports.Ports;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

// 明细类报表（行程明细、取消）逐行返回；汇总类报表按 ReportRowMergeStages 在查询时合并后再搜索、排序、分页。
internal sealed class MongoReportRowReader(IMongoDatabase database) : IReportRowReader
{
    private static readonly Collation SortCollation = new("en", strength: CollationStrength.Secondary);

    private const string PayloadPrefix = "payload.";
    private const string RowIdField = "rowId";

    // 每类报表预览里可见的文本列，搜索只在这些列上做不区分大小写的包含匹配。
    private static readonly IReadOnlyDictionary<ReportType, string[]> SearchFields = new Dictionary<ReportType, string[]>
    {
        [ReportType.DailyJobSummary] = ["propertyId", "redemptionSummary"],
        [ReportType.TripDetail] =
        [
            "tripReference", "propertyId", "guestName", "driverName", "vehicleDescription", "loyalty.redemptionId", "loyalty.entitlement",
            "loyalty.redemptionState", "source"
        ],
        [ReportType.DriverUtilization] = ["driverName", "propertyId", "redemptionContributionReference"],
        [ReportType.VehicleUtilization] = ["vehicleDescription", "redemptionReference"],
        [ReportType.Cancellation] = ["reasonCode", "reason", "source", "pointsReturnPolicyCode", "pointsReturnReference"]
    };

    private IMongoCollection<ReportRowDocument> Collection =>
        database.GetCollection<ReportRowDocument>(ReportingCollections.ReportRows);

    public Task<PageResult<ReportRow>> ListAsync(ReportRowsQuery query, CancellationToken cancellationToken) =>
        ReportRowMergeStages.IsMerged(query.Type) ? ListMergedAsync(query, cancellationToken) : ListRowsAsync(query, cancellationToken);

    private async Task<PageResult<ReportRow>> ListRowsAsync(ReportRowsQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            Builders<ReportRowDocument>.Filter.And(BuildFilter(query), SearchOf<ReportRowDocument>(query)),
            MongoSortFactory.Create<ReportRowDocument>(query.List.Sort, SortFieldsOf(query.Type)),
            query.List,
            cancellationToken);

        return new PageResult<ReportRow>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    private async Task<PageResult<ReportRow>> ListMergedAsync(ReportRowsQuery query, CancellationToken cancellationToken)
    {
        var merged = Collection.Aggregate(new AggregateOptions { Collation = SortCollation })
            .Match(BuildFilter(query))
            .Sort(Builders<ReportRowDocument>.Sort.Ascending(document => document.Filter.From))
            .Group<BsonDocument>(ReportRowMergeStages.Group(query.Type))
            .Project<BsonDocument>(ReportRowMergeStages.Project(query.Type, query.ServiceType))
            .Match(SearchOf<BsonDocument>(query));
        var total = await merged.Count().FirstOrDefaultAsync(cancellationToken);
        var documents = await merged
            .Sort(MongoSortFactory.Create<BsonDocument>(query.List.Sort, SortFieldsOf(query.Type)))
            .Skip(query.List.Skip)
            .Limit(query.List.PageSize)
            .ToListAsync(cancellationToken);

        var rows = documents.Select(document => BsonSerializer.Deserialize<ReportRowDocument>(document).ToDomain()).ToList();

        return new PageResult<ReportRow>(rows, checked((int)(total?.Count ?? 0)));
    }

    // API 排序字段与载荷字段同名（loyalty.* 对应 payload.loyalty.*），只有 rowId 映射到文档标识。
    private static IReadOnlyDictionary<string, string> SortFieldsOf(ReportType type) =>
        ReportSorting.Of(type).Fields.ToDictionary(field => field, field => field == RowIdField ? "_id" : PayloadPrefix + field);

    private static FilterDefinition<ReportRowDocument> BuildFilter(ReportRowsQuery query)
    {
        var filter = Builders<ReportRowDocument>.Filter;
        var propertyId = EffectivePropertyId(query);

        return new MongoFilterBuilder<ReportRowDocument>(filter.Eq(candidate => candidate.RowType, WireEnum.ToWire(query.Type)))
            .When(propertyId is not null, () => filter.Eq(candidate => candidate.Filter.PropertyId, propertyId))
            .When(query.From is not null, () => filter.Gte(candidate => candidate.Filter.From, UtcDateTimes.ToStored(query.From!.Value)))
            .When(query.To is not null, () => filter.Lte(candidate => candidate.Filter.To, UtcDateTimes.ToStored(query.To!.Value)))
            .When(query.ServiceType is not null, () => filter.Eq(candidate => candidate.Filter.ServiceType, WireEnum.ToWire(query.ServiceType!.Value)))
            .When(IsDriverTotals(query), () => filter.Eq(candidate => candidate.Filter.ServiceType, null))
            .Build();
    }

    // 司机利用率另有不分服务类型的总计行（含空闲与加班）：不按服务类型筛选时只读总计行，按服务类型筛选时只读该服务的行。
    private static bool IsDriverTotals(ReportRowsQuery query) => query.Type == ReportType.DriverUtilization && query.ServiceType is null;

    private static FilterDefinition<TDocument> SearchOf<TDocument>(ReportRowsQuery query) =>
        query.List.HasSearch
            ? MongoTextSearch.Contains<TDocument>(SearchFields[query.Type].Select(field => PayloadPrefix + field), query.List.Search)
            : Builders<TDocument>.Filter.Empty;

    // 非全物业调用者只看到自己物业的行；显式物业筛选已由用例确认在授权范围内。
    private static string? EffectivePropertyId(ReportRowsQuery query) =>
        query.PropertyId ?? (query.Scope.IsAllProperties ? null : query.Scope.PropertyId);
}

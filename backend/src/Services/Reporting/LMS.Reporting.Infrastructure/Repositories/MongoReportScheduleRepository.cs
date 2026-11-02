using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Schedules.Models;
using LMS.Reporting.Application.Schedules.Ports;
using LMS.Reporting.Domain.Schedules;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using LMS.Reporting.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

internal sealed class MongoReportScheduleRepository(IMongoDatabase database) : IReportScheduleRepository
{
    private static readonly string[] SearchFields = ["recipientEmail", "frequency", "reportType"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["scheduleId"] = "_id",
        ["recipientEmail"] = "recipientEmail",
        ["frequency"] = "frequency",
        ["reportType"] = "reportType"
    };

    private IMongoCollection<ReportScheduleDocument> Collection =>
        database.GetCollection<ReportScheduleDocument>(ReportingCollections.ReportSchedules);

    public async Task<ReportSchedule?> FindAsync(string scheduleId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.ScheduleId == scheduleId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ReportSchedule?> FindAsync(string scheduleId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var document = await Collection.Find(session, candidate => candidate.ScheduleId == scheduleId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<PageResult<ReportSchedule>> ListAsync(ListReportSchedulesQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<ReportScheduleDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<ReportSchedule>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public Task InsertAsync(ReportSchedule schedule, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection, MongoTransactionContext.RequireSession(context), ReportScheduleDocument.FromDomain(schedule), cancellationToken);

    public async Task<bool> DeleteAsync(string scheduleId, ITransactionContext context, CancellationToken cancellationToken)
    {
        var result = await Collection.DeleteOneAsync(
            MongoTransactionContext.RequireSession(context), candidate => candidate.ScheduleId == scheduleId, cancellationToken: cancellationToken);

        return result.DeletedCount == 1;
    }

    // 全物业调用者看到所有计划；单一物业调用者只看到登记在自己物业上的计划，全物业登记的计划对其不可见。
    private static FilterDefinition<ReportScheduleDocument> BuildFilter(ListReportSchedulesQuery query)
    {
        var filter = Builders<ReportScheduleDocument>.Filter;

        return new MongoFilterBuilder<ReportScheduleDocument>(FilterDefinition<ReportScheduleDocument>.Empty)
            .When(!query.Scope.IsAllProperties, () => filter.Eq(candidate => candidate.ScopeKey, query.Scope.PropertyId))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<ReportScheduleDocument>(SearchFields, query.List.Search))
            .Build();
    }
}

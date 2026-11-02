using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Infrastructure.Documents;
using LMS.Reporting.Infrastructure.Persistence;
using LMS.Reporting.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Reporting.Infrastructure.Repositories;

// 一个业务日的报表行整体替换：同一事务内先删该日全部类型的旧行（按统计窗口起点，走 ix_window_from），再批量写入新行。
internal sealed class MongoReportRowWriter(IMongoDatabase database) : IReportRowWriter
{
    private IMongoCollection<ReportRowDocument> Collection =>
        database.GetCollection<ReportRowDocument>(ReportingCollections.ReportRows);

    public async Task ReplaceDayAsync(DateTimeOffset dayStart, IReadOnlyList<ReportRow> rows, ITransactionContext context, CancellationToken cancellationToken)
    {
        var session = MongoTransactionContext.RequireSession(context);
        var filter = Builders<ReportRowDocument>.Filter.Eq(document => document.Filter.From, UtcDateTimes.ToStored(dayStart));
        await Collection.DeleteManyAsync(session, filter, cancellationToken: cancellationToken);
        if (rows.Count > 0)
        {
            await Collection.InsertManyAsync(session, rows.Select(ReportRowDocument.From), cancellationToken: cancellationToken);
        }
    }
}

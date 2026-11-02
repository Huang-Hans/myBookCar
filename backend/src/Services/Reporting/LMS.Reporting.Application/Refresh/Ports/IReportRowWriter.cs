using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Refresh.Ports;

public interface IReportRowWriter
{
    // 在同一事务内删除统计窗口起点为 dayStart 的全部报表行，再写入 rows，使一个业务日的五类报表整体替换。
    Task ReplaceDayAsync(DateTimeOffset dayStart, IReadOnlyList<ReportRow> rows, ITransactionContext context, CancellationToken cancellationToken);
}

using LMS.Reporting.Application.Refresh.Models;

namespace LMS.Reporting.Application.Refresh.Handlers;

// 重算业务日闭区间 [from, to] 的五类报表行。
public interface IRefreshReportRowsHandler
{
    Task<ReportRefreshResult> RefreshAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Querying;
using LMS.Reporting.Application.Reports.Models;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Reports.Ports;

public interface IReportRowReader
{
    // 非全物业调用者只能看到自己物业的行；没有物业归属的行不存在（持久行的物业必须非空）。
    Task<PageResult<ReportRow>> ListAsync(ReportRowsQuery query, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Reporting.Application.Reports.Models;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Reports.Handlers;

public interface IReportQueryHandler
{
    Task<OperationResult<PageResult<ReportRow>>> ListAsync(ReportRowsQuery query, CancellationToken cancellationToken);
}

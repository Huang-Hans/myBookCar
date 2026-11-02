using LMS.BuildingBlocks.Http.Requests;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Contracts.Reports;

public sealed class DailyJobSummaryListRequest : ReportListRequest
{
    private static readonly ListQuerySpec Spec = SpecOf(ReportType.DailyJobSummary);

    public override ListQuerySpec GetSortSpecification() => Spec;
}

using LMS.BuildingBlocks.Http.Requests;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Contracts.Reports;

public sealed class CancellationReportListRequest : ReportListRequest
{
    private static readonly ListQuerySpec Spec = SpecOf(ReportType.Cancellation);

    public override ListQuerySpec GetSortSpecification() => Spec;
}

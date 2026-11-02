using LMS.BuildingBlocks.Http.Requests;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Contracts.Reports;

public sealed class TripDetailListRequest : ReportListRequest
{
    private static readonly ListQuerySpec Spec = SpecOf(ReportType.TripDetail);

    public override ListQuerySpec GetSortSpecification() => Spec;
}

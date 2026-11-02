using LMS.BuildingBlocks.Http.Requests;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Contracts.Reports;

public sealed class DriverUtilizationListRequest : ReportListRequest
{
    private static readonly ListQuerySpec Spec = SpecOf(ReportType.DriverUtilization);

    public override ListQuerySpec GetSortSpecification() => Spec;
}

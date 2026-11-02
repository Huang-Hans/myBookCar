using LMS.BuildingBlocks.Http.Requests;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Contracts.Reports;

public sealed class VehicleUtilizationListRequest : ReportListRequest
{
    private static readonly ListQuerySpec Spec = SpecOf(ReportType.VehicleUtilization);

    public override ListQuerySpec GetSortSpecification() => Spec;
}

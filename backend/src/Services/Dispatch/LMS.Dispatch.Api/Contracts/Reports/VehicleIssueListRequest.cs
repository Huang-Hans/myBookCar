using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Api.Contracts.Reports;

public sealed class VehicleIssueListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(
        ReportSorting.VehicleIssueFields, ReportSorting.VehicleIssueDefaultSort, ReportSorting.VehicleIssueStableField);

    [Description("按处理状态筛选：RECORDED / FORWARDED / ACKNOWLEDGED。")]
    public VehicleIssueStatus? Status { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}

using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Api.Contracts.Reports;

public sealed class DriverIncidentListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(
        ReportSorting.IncidentFields, ReportSorting.IncidentDefaultSort, ReportSorting.IncidentStableField);

    [Description("按事件类型筛选。")]
    public IncidentType? Type { get; set; }

    [Description("按处理状态筛选：RECORDED / FORWARDED / ACKNOWLEDGED。")]
    public IncidentStatus? Status { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}

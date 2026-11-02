using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Reporting.Application.Schedules.Models;

namespace LMS.Reporting.Api.Contracts.Schedules;

public sealed class ReportScheduleListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec =
        new(ReportScheduleSorting.Fields, ReportScheduleSorting.DefaultSort, ReportScheduleSorting.StableField);

    public override ListQuerySpec GetSortSpecification() => Spec;
}

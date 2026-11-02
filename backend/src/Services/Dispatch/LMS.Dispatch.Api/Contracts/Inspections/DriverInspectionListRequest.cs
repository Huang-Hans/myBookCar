using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Inspections.Models;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Api.Contracts.Inspections;

public sealed class DriverInspectionListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(InspectionSorting.Fields, InspectionSorting.DefaultSort, InspectionSorting.StableField);

    [Description("按阶段筛选：PRE_TRIP / POST_TRIP。")]
    public InspectionPhase? Phase { get; set; }

    [Description("按是否存在不合格项筛选；不传时不过滤。")]
    public bool? HasFailure { get; set; }

    [Description("提交时间下限（含），ISO 8601 UTC 并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Description("提交时间上限（不含），ISO 8601 UTC 并以 Z 结尾；与 from 同时给出时必须晚于 from。")]
    public DateTimeOffset? To { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}

using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.AuditRecords.Models;

namespace LMS.Platform.Api.Contracts.Audit;

public sealed class AuditRecordListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(AuditRecordSorting.Fields, AuditRecordSorting.DefaultSort, AuditRecordSorting.StableField);

    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    [Description("起始时间（含），ISO 8601 UTC 时间并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Description("结束时间（不含），ISO 8601 UTC 时间并以 Z 结尾。")]
    public DateTimeOffset? To { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}

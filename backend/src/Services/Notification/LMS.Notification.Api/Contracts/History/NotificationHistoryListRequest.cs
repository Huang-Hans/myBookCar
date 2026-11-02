using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Notification.Application.History.Models;
using LMS.Notification.Domain.History;

namespace LMS.Notification.Api.Contracts.History;

public sealed class NotificationHistoryListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec =
        new(NotificationHistorySorting.Fields, NotificationHistorySorting.DefaultSort, NotificationHistorySorting.StableField);

    [Description("按投递状态筛选：SENT、DELIVERED、FAILED；不传时不过滤。")]
    public NotificationStatus? Status { get; set; }

    [Description("起始时间（含），ISO 8601 UTC 时间并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Description("结束时间（不含），ISO 8601 UTC 时间并以 Z 结尾；与 from 同时给出时必须晚于 from。")]
    public DateTimeOffset? To { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}

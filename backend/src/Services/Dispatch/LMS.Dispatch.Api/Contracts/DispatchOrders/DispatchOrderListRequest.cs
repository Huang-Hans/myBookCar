using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Orders.Models;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

public sealed class DispatchOrderListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(DispatchOrderSorting.Fields, DispatchOrderSorting.DefaultSort, DispatchOrderSorting.StableField);

    [Description("按物业筛选；传入时不能为空白，且必须在调用者授权范围内。")]
    public string? PropertyId { get; set; }

    [Description("按下单渠道筛选：CONSOLE（客服代订）/ SELF_SERVICE（客户自助）。")]
    public OrderChannel? Channel { get; set; }

    [Description("按派单状态筛选：PENDING（待派单）/ AWAITING_RESPONSE / PROCESSING / DRIVER_ASSIGNED / NEEDS_REDISPATCH / COMPLETED / CANCELLED。PENDING_CONFIRMATION 为兼容值，返回空列表。")]
    public DispatchStatus? DispatchStatus { get; set; }

    [Description("接车时间下限（含），ISO 8601 UTC 并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Description("接车时间上限（不含），ISO 8601 UTC 并以 Z 结尾；与 from 同时给出时必须晚于 from。")]
    public DateTimeOffset? To { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}

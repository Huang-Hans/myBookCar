using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

public sealed class DispatchOrderSummaryRequest : QueryRequest
{
    [UnrestrictedText]
    [Description("在可见字段中进行不区分大小写的搜索；空白表示不筛选。")]
    public string Search { get; set; } = string.Empty;

    [Description("按物业筛选；传入时不能为空白，且必须在调用者授权范围内。")]
    public string? PropertyId { get; set; }

    [Description("按下单渠道筛选：CONSOLE / SELF_SERVICE。")]
    public OrderChannel? Channel { get; set; }

    [Description("接车时间下限（含），ISO 8601 UTC 并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Description("接车时间上限（不含），ISO 8601 UTC 并以 Z 结尾；与 from 同时给出时必须晚于 from。")]
    public DateTimeOffset? To { get; set; }
}

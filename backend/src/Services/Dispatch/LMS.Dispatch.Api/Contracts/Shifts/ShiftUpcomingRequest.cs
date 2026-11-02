using System.ComponentModel;

namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// 未来 15 天排班列表。起点固定为澳门业务当天，不能改范围。
/// </summary>
public sealed class ShiftUpcomingRequest
{
    [Description("按物业筛选。省略表示调用者授权范围内的全部地点。")]
    public string? PropertyId { get; set; }
}

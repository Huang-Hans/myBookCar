using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// 班次看板查询。日期省略时按澳门业务时区取当天。
/// </summary>
public sealed class ShiftBoardRequest
{
    [FromQuery(Name = "pickupAt")]
    [Description("ISO 8601 UTC pickup time to seconds, e.g. 2026-10-30T16:00:00Z. Returns only the matching on-duty period. Before 07:00 uses the previous business date's night shift. Mutually exclusive with date.")]
    public string? PickupAt { get; set; }

    [Description("看板业务日期，YYYY-MM-DD。省略时取澳门时区的当天。夜班仍归开始那天。")]
    public DateOnly? Date { get; set; }

    [Description("按物业筛选。省略表示调用者授权范围内的全部地点。")]
    public string? PropertyId { get; set; }
}

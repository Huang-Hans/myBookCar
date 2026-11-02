using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Booking.Api.Contracts.ReportingSources;

public sealed class ReportingTripFactsRequest : QueryRequest
{
    [Required]
    [Description("计划接车时间区间起点（含），ISO 8601 UTC 并以 Z 结尾。")]
    public DateTimeOffset? From { get; set; }

    [Required]
    [Description("计划接车时间区间终点（不含），ISO 8601 UTC 并以 Z 结尾；必须晚于 from。")]
    public DateTimeOffset? To { get; set; }

    [Description("游标：只返回行程标识大于该值的行程；首批不传。")]
    public string? After { get; set; }

    [Range(1, 500, ErrorMessage = "Must be between 1 and 500")]
    [Description("本批最多返回的行程数，默认 200。")]
    public int Limit { get; set; } = 200;
}

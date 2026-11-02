using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Dispatch.Api.Contracts.ReportingSources;

public sealed class ReportingShiftsRequest : QueryRequest
{
    [Required]
    [Description("业务日期起点（含），YYYY-MM-DD。")]
    public string? From { get; set; }

    [Required]
    [Description("业务日期终点（含），YYYY-MM-DD；不早于 from，跨度不超过 62 天。")]
    public string? To { get; set; }
}

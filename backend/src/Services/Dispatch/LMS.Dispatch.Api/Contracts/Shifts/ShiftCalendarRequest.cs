using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>
/// Queries shift boards for an inclusive range of at most 31 business dates.
/// </summary>
public sealed class ShiftCalendarRequest : QueryRequest
{
    [Required]
    [Description("First business date, inclusive, in YYYY-MM-DD format.")]
    public string? From { get; set; }

    [Required]
    [Description("Last business date, inclusive, in YYYY-MM-DD format. The range must contain at most 31 dates.")]
    public string? To { get; set; }

    [Description("Optional property filter within the caller's authorized scope.")]
    public string? PropertyId { get; set; }
}

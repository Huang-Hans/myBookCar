using System.ComponentModel;

namespace LMS.Dispatch.Api.Contracts.Shifts;

public class ShiftBatchRequest
{
    [Description("Optional filter for the selected drivers' current property. Never stored on shifts.")]
    public string? PropertyId { get; init; }
    [Description("Unique driver IDs. At most 1000 driver-date combinations per batch.")]
    public IReadOnlyList<string?>? DriverIds { get; init; }
    [Description("First inclusive business date in YYYY-MM-DD format.")]
    public string? From { get; init; }
    [Description("Last inclusive business date. At most 31 calendar days.")]
    public string? To { get; init; }
    [Description("Unique ISO weekdays: Monday = 1, Sunday = 7.")]
    public IReadOnlyList<int>? Weekdays { get; init; }
    [Description("MORNING, AFTERNOON or NIGHT.")]
    public string? Period { get; init; }
    [Description("ON_DUTY, REST or LEAVE.")]
    public string? Status { get; init; }
    [Description("PREFERRED or STANDBY.")]
    public string? Role { get; init; }
}

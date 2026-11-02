using System.Text.Json;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Models;

public sealed record ShiftBatchRules(
    string? PropertyId,
    IReadOnlyList<string> DriverIds,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<int> Weekdays,
    ShiftPeriod Period,
    ShiftDutyStatus DutyStatus,
    ShiftRole Role)
{
    // Weekdays follow ISO numbering: Monday = 1, Sunday = 7.
    public IReadOnlyList<DateOnly> Dates() => Enumerable.Range(0, To.DayNumber - From.DayNumber + 1)
        .Select(From.AddDays)
        .Where(date => Weekdays.Contains(((int)date.DayOfWeek + 6) % 7 + 1))
        .ToList();

    public string ToCanonicalForm() => JsonSerializer.Serialize(this with
    {
        DriverIds = DriverIds.Order(StringComparer.Ordinal).ToList(),
        Weekdays = Weekdays.Order().ToList()
    });
}

using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Schedules.Commands;

public sealed record CreateReportScheduleCommand(
    string RecipientEmail,
    string Frequency,
    ReportType ReportType,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[] { RecipientEmail.Trim().ToLowerInvariant(), Frequency, WireEnum.ToWire(ReportType) });
}

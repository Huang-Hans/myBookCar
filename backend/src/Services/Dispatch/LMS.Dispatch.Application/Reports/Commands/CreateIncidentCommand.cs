using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Commands;

public sealed record CreateIncidentCommand(
    string DriverId,
    IncidentType Type,
    DateTimeOffset OccurredAt,
    string Description,
    string? TripId,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[] { DriverId, WireEnum.ToWire(Type), OccurredAt.ToString("O"), Description, TripId });
}

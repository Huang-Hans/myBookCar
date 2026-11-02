using System.Text.Json;
using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Reports.Commands;

public sealed record CreateVehicleIssueCommand(
    string DriverId,
    string Description,
    string? InspectionId,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() => JsonSerializer.Serialize(new object?[] { DriverId, Description, InspectionId });
}

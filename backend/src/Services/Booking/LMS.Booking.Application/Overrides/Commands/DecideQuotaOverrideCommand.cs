using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Common;

namespace LMS.Booking.Application.Overrides.Commands;

public sealed record DecideQuotaOverrideCommand(
    string OverrideId,
    long ExpectedVersion,
    ReviewDecision Decision,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[] { OverrideId, ExpectedVersion, WireEnum.ToWire(Decision) });
}

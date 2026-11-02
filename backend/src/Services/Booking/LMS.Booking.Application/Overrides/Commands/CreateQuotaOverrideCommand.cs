using System.Text.Json;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Overrides.Commands;

public sealed record CreateQuotaOverrideCommand(
    string PropertyId,
    string GuestId,
    int AdditionalDailyEntitlements,
    string Justification,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[] { PropertyId, GuestId, AdditionalDailyEntitlements, Justification });
}

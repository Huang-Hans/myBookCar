using System.Text.Json;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Trips.Commands;

public sealed record CancelTripCommand(
    string TripId,
    long ExpectedVersion,
    string ReasonCode,
    string Comments,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() => JsonSerializer.Serialize(new object?[] { TripId, ExpectedVersion, ReasonCode, Comments });
}

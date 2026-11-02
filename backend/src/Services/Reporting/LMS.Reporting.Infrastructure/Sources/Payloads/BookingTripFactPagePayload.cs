namespace LMS.Reporting.Infrastructure.Sources.Payloads;

internal sealed record BookingTripFactPagePayload(IReadOnlyList<BookingTripFactPayload>? Items, string? NextAfter);

namespace LMS.Reporting.Infrastructure.Sources.Payloads;

internal sealed record BookingTripPointsPayload(long Required, string? Reference, string State);

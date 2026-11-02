namespace LMS.Reporting.Infrastructure.Sources.Payloads;

internal sealed record BookingTripCancellationPayload(string ReasonCode, string CancelledBy, DateTimeOffset CancelledAt, string? PolicyReference);

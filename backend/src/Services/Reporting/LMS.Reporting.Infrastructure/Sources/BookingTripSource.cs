using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Domain.Sources;
using LMS.Reporting.Infrastructure.ServiceIdentity;
using LMS.Reporting.Infrastructure.Sources.Payloads;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Infrastructure.Sources;

// 按 after 游标分批读完 Booking 的行程事实，并转换为报表领域的来源事实。
internal sealed class BookingTripSource(HttpClient client, IServiceTokenIssuer tokens, IOptions<SourceClientOptions> options) : IBookingTripSource
{
    private const string SourceName = "booking";
    private const string CompletedStatus = "COMPLETED";
    private const string CancelledStatus = "CANCELLED";

    public async Task<IReadOnlyList<ReportTripFact>> ListAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var facts = new List<ReportTripFact>();
        string? after = null;
        do
        {
            var page = await SourceHttp.GetAsync<BookingTripFactPagePayload>(client, tokens, PathOf(from, to, after), SourceName, cancellationToken);
            facts.AddRange((page.Items ?? []).Select(ToFact));
            after = page.NextAfter;
        }
        while (after is not null);

        return facts;
    }

    private string PathOf(DateTimeOffset from, DateTimeOffset to, string? after)
    {
        var path = $"internal/v1/reporting/trip-facts?from={SourceHttp.Instant(from)}&to={SourceHttp.Instant(to)}&limit={options.Value.BookingBatchSize}";

        return after is null ? path : $"{path}&after={Uri.EscapeDataString(after)}";
    }

    private static ReportTripFact ToFact(BookingTripFactPayload trip) => new(
        trip.TripId,
        trip.TripReference,
        trip.PropertyId,
        trip.GuestId,
        trip.GuestName,
        trip.PickupAt,
        OutcomeOf(trip.Status),
        ServiceTypes.Of(trip.IsCrossBorder, trip.Product),
        trip.Points.Required,
        trip.Points.Reference,
        trip.Points.State,
        trip.Assignment?.DriverId,
        trip.Assignment?.DriverName,
        trip.Assignment?.VehicleLabel,
        trip.EnRouteAt,
        trip.PickedUpAt,
        trip.CompletedAt,
        trip.Cancellation is null
            ? null
            : new ReportTripCancellation(trip.Cancellation.ReasonCode, trip.Cancellation.CancelledBy, trip.Cancellation.CancelledAt, trip.Cancellation.PolicyReference),
        trip.PickupToCompleteMeters,
        trip.EnRouteToCompleteMeters);

    private static TripOutcome OutcomeOf(string status) => status switch
    {
        CompletedStatus => TripOutcome.Completed,
        CancelledStatus => TripOutcome.Cancelled,
        _ => TripOutcome.Open
    };
}

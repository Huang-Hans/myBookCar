using System.Globalization;
using System.Net.Http.Json;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Infrastructure.Booking;

// 通过 Booking 的服务间端点实时读取派单队列：订单不在 Dispatch 复制，转发调度员自己的令牌，范围与权限由 Booking 重新检查。
internal sealed class BookingDispatchOrdersClient(HttpClient client, ICallerToken callerToken) : IBookingDispatchOrders
{
    private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    public Task<BookingResult<PageResult<BookingOrder>>> ListAsync(BookingOrderQuery query, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, $"internal/v1/dispatch-orders/queries?{ListQueryString(query)}")
        {
            Content = JsonContent.Create(new { includeTripIds = query.IncludeTripIds, excludeTripIds = query.ExcludeTripIds })
        };

        return BookingHttp.SendEnvelopeAsync<List<DispatchOrderPayload>, PageResult<BookingOrder>>(
            client,
            callerToken,
            message,
            envelope => new PageResult<BookingOrder>(
                envelope.Data!.Select(DispatchOrderPayloadMapper.ToOrder).ToList(),
                envelope.Meta?.Pagination?.TotalItems ?? envelope.Data!.Count),
            cancellationToken);
    }

    public Task<BookingResult<BookingOrderCounts>> CountAsync(BookingOrderCountQuery query, CancellationToken cancellationToken)
    {
        var filter = FilterQueryString(query.Search, query.PropertyId, query.Channel, query.From, query.To);
        var message = new HttpRequestMessage(HttpMethod.Post, $"internal/v1/dispatch-order-summaries?{filter}")
        {
            Content = JsonContent.Create(new
            {
                awaitingTripIds = query.Sets.Awaiting,
                processingTripIds = query.Sets.Processing,
                redispatchTripIds = query.Sets.Redispatch
            })
        };

        return BookingHttp.SendAsync<DispatchOrderCountsPayload, BookingOrderCounts>(
            client, callerToken, message, DispatchOrderPayloadMapper.ToCounts, cancellationToken);
    }

    public Task<BookingResult<BookingOrder>> GetAsync(string tripId, CancellationToken cancellationToken) =>
        BookingHttp.SendAsync<DispatchOrderPayload, BookingOrder>(
            client,
            callerToken,
            new HttpRequestMessage(HttpMethod.Get, $"internal/v1/dispatch-orders/{Uri.EscapeDataString(tripId)}"),
            DispatchOrderPayloadMapper.ToOrder,
            cancellationToken);

    private static string ListQueryString(BookingOrderQuery query)
    {
        var paging = $"page={query.List.Page}&pageSize={query.List.PageSize}&sort={Uri.EscapeDataString(SortText(query.List.Sort))}&group={WireEnum.ToWire(query.Group)}";
        var filter = FilterQueryString(query.List.Search, query.PropertyId, query.Channel, query.From, query.To);

        return filter.Length == 0 ? paging : $"{paging}&{filter}";
    }

    private static string FilterQueryString(string search, string? propertyId, OrderChannel? channel, DateTimeOffset? from, DateTimeOffset? to)
    {
        var parts = new List<string>();
        AddIf(parts, "search", search.Length > 0 ? search : null);
        AddIf(parts, "propertyId", propertyId);
        AddIf(parts, "channel", channel is null ? null : WireEnum.ToWire(channel.Value));
        AddIf(parts, "from", from?.UtcDateTime.ToString(UtcFormat, CultureInfo.InvariantCulture));
        AddIf(parts, "to", to?.UtcDateTime.ToString(UtcFormat, CultureInfo.InvariantCulture));

        return string.Join('&', parts);
    }

    private static void AddIf(List<string> parts, string name, string? value)
    {
        if (value is not null)
        {
            parts.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }

    // Dispatch 对外的 orderStatus 在 Booking 里叫 status；其余字段同名。
    private static string SortText(IReadOnlyList<SortTerm> terms) =>
        string.Join(',', terms.Select(term => (term.IsDescending ? "-" : string.Empty) + (term.Field == "orderStatus" ? "status" : term.Field)));
}

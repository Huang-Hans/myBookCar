using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Directory;

namespace LMS.Dispatch.Infrastructure.Booking;

// 对 Booking 内部端点的共用调用：转发终端用户自己的令牌，Booking 因此重新检查其权限与本人身份。
// 网络失败、超时和无法理解的响应都视为 Booking 不可用，而不是订单不存在或冲突。
internal static class BookingHttp
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Task<BookingResult<TValue>> SendAsync<TPayload, TValue>(
        HttpClient client,
        ICallerToken callerToken,
        HttpRequestMessage request,
        Func<TPayload, TValue> map,
        CancellationToken cancellationToken) =>
        SendEnvelopeAsync<TPayload, TValue>(client, callerToken, request, envelope => map(envelope.Data!), cancellationToken);

    public static async Task<BookingResult<TValue>> SendEnvelopeAsync<TPayload, TValue>(
        HttpClient client,
        ICallerToken callerToken,
        HttpRequestMessage request,
        Func<BookingEnvelope<TPayload>, TValue> map,
        CancellationToken cancellationToken)
    {
        using (request)
        {
            if (callerToken.Value is null)
            {
                return BookingResult<TValue>.Failed(BookingOutcome.Forbidden);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", callerToken.Value);
            using var response = await TrySendAsync(client, request, cancellationToken);

            return response is null
                ? BookingResult<TValue>.Failed(BookingOutcome.Unavailable)
                : await ReadAsync(response, map, cancellationToken);
        }
    }

    private static async Task<HttpResponseMessage?> TrySendAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static async Task<BookingResult<TValue>> ReadAsync<TPayload, TValue>(
        HttpResponseMessage response,
        Func<BookingEnvelope<TPayload>, TValue> map,
        CancellationToken cancellationToken) =>
        response.StatusCode switch
        {
            HttpStatusCode.OK or HttpStatusCode.Created => await ReadSuccessAsync(response, map, cancellationToken),
            HttpStatusCode.NotFound => BookingResult<TValue>.Failed(BookingOutcome.NotFound),
            HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity => BookingResult<TValue>.Failed(BookingOutcome.Conflict),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => BookingResult<TValue>.Failed(BookingOutcome.Forbidden),
            _ => BookingResult<TValue>.Failed(BookingOutcome.Unavailable)
        };

    private static async Task<BookingResult<TValue>> ReadSuccessAsync<TPayload, TValue>(
        HttpResponseMessage response,
        Func<BookingEnvelope<TPayload>, TValue> map,
        CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<BookingEnvelope<TPayload>>(Json, cancellationToken);

            return envelope is { Data: not null }
                ? BookingResult<TValue>.Success(map(envelope))
                : BookingResult<TValue>.Failed(BookingOutcome.Unavailable);
        }
        catch (JsonException)
        {
            return BookingResult<TValue>.Failed(BookingOutcome.Unavailable);
        }
    }
}

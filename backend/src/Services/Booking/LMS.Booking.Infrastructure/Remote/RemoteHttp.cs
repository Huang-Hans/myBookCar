using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Directory;

namespace LMS.Booking.Infrastructure.Remote;

// 对 Platform / Dispatch 服务间调用的公共部分：转发终端用户令牌，把响应映射为 DirectoryResult。
// 网络失败、超时和无法理解的响应都视为对方不可用，而不是无权限或不存在。
internal static class RemoteHttp
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<HttpResponseMessage?> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public static Task<DirectoryResult<TValue>> ReadAsync<TPayload, TValue>(
        HttpResponseMessage? response,
        Func<TPayload, TValue> map,
        CancellationToken cancellationToken) =>
        ReadEnvelopeAsync<TPayload, TValue>(response, envelope => map(envelope.Data!), cancellationToken);

    // 需要同时读取响应外壳（例如 meta.pagination）时使用；map 只在 data 存在时调用。
    public static async Task<DirectoryResult<TValue>> ReadEnvelopeAsync<TPayload, TValue>(
        HttpResponseMessage? response,
        Func<RemoteEnvelope<TPayload>, TValue> map,
        CancellationToken cancellationToken)
    {
        if (response is null)
        {
            return DirectoryResult<TValue>.Failed(DirectoryOutcome.Unavailable);
        }

        using (response)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.OK or HttpStatusCode.Created => await ReadSuccessAsync(response, map, cancellationToken),
                HttpStatusCode.NotFound => DirectoryResult<TValue>.Failed(DirectoryOutcome.NotFound),
                HttpStatusCode.Conflict => DirectoryResult<TValue>.Failed(DirectoryOutcome.Conflict),
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => DirectoryResult<TValue>.Failed(DirectoryOutcome.Forbidden),
                HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => await ReadRejectionAsync<TValue>(response, cancellationToken),
                _ => DirectoryResult<TValue>.Failed(DirectoryOutcome.Unavailable)
            };
        }
    }

    private static async Task<DirectoryResult<TValue>> ReadSuccessAsync<TPayload, TValue>(
        HttpResponseMessage response,
        Func<RemoteEnvelope<TPayload>, TValue> map,
        CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<RemoteEnvelope<TPayload>>(Json, cancellationToken);

            return envelope is { Data: not null }
                ? DirectoryResult<TValue>.Success(map(envelope))
                : DirectoryResult<TValue>.Failed(DirectoryOutcome.Unavailable);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return DirectoryResult<TValue>.Failed(DirectoryOutcome.Unavailable);
        }
    }

    private static async Task<DirectoryResult<TValue>> ReadRejectionAsync<TValue>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await response.Content.ReadFromJsonAsync<RemoteEnvelope<object>>(Json, cancellationToken);
            var issues = envelope?.Errors?.Select(error => new FieldIssue(error.Field, error.Message)).ToList() ?? [];

            return DirectoryResult<TValue>.Rejected(issues);
        }
        catch (JsonException)
        {
            return DirectoryResult<TValue>.Failed(DirectoryOutcome.Unavailable);
        }
    }
}

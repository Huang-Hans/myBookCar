using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Directory;
using Microsoft.Extensions.Hosting;

namespace LMS.Dispatch.Infrastructure.Platform;

// 通过 Platform 的服务间端点解析调用者授权并核对物业代码。转发的是终端用户自己的令牌，
// 因此 Platform 只会回答该用户自己的授权，Dispatch 不持有任何额外的服务凭据。
internal sealed class PlatformDirectory(HttpClient client, ICallerToken callerToken, IHostEnvironment environment) : IPlatformDirectory
{
    private const string CallerAuthorizationPath = "internal/v1/caller-authorization";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<CallerLookup> ResolveCallerAsync(CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return new CallerLookup(CallerLookupOutcome.NotLinked, null);
        }

        using var response = await SendAsync(CallerAuthorizationPath, cancellationToken);

        return response?.StatusCode switch
        {
            HttpStatusCode.OK => await ReadCallerAsync(response, cancellationToken),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new CallerLookup(CallerLookupOutcome.NotLinked, null),
            _ => new CallerLookup(CallerLookupOutcome.SourceUnavailable, null)
        };
    }

    public async Task<PropertyLookup> ContainsPropertyAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            // 本机 Swagger 不带令牌，无法代用户询问 Platform。开发环境按调用者已有的物业范围放行；正式环境没有令牌到不了这里。
            return environment.IsDevelopment() ? PropertyLookup.Found : PropertyLookup.SourceUnavailable;
        }

        using var response = await SendAsync($"internal/v1/properties/{Uri.EscapeDataString(propertyId)}", cancellationToken);

        return response?.StatusCode switch
        {
            HttpStatusCode.NoContent => PropertyLookup.Found,
            HttpStatusCode.NotFound => PropertyLookup.NotFound,
            _ => PropertyLookup.SourceUnavailable
        };
    }

    // 网络失败和超时都视为 Platform 暂时不可用（返回 null），而不是把它误判成无权限或不存在。
    private async Task<HttpResponseMessage?> SendAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", callerToken.Value);
        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static async Task<CallerLookup> ReadCallerAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var envelope = await response.Content.ReadFromJsonAsync<PlatformEnvelope<PlatformCallerPayload>>(Json, cancellationToken);
        if (envelope?.Data is not { } payload)
        {
            return new CallerLookup(CallerLookupOutcome.SourceUnavailable, null);
        }

        var caller = new PlatformCaller(payload.UserId, payload.RoleCode, payload.HasAllPropertyAccess, payload.PropertyId, payload.Permissions);

        return new CallerLookup(CallerLookupOutcome.Found, caller);
    }
}

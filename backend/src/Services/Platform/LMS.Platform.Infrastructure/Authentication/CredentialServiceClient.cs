using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Infrastructure.Authentication;

// 调用数据所属服务（司机 → Dispatch，客户 → Booking）的内部凭据核对接口。请求以 Platform 服务令牌认证，
// 响应只包含核对结论与业务实体标识，不包含密码哈希。非 200、网络失败、超时或无法理解的响应一律按“不可用”处理，
// 不会被当成凭据正确，也不会被当成凭据错误。明文密码只放在请求正文里，不写日志。
internal abstract class CredentialServiceClient(HttpClient client, AppTokenSigner signer)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected abstract string VerificationPath { get; }

    protected abstract string EligibilityPath { get; }

    public async Task<CredentialCheck> VerifyAsync(string phone, string password, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, VerificationPath)
        {
            Content = JsonContent.Create(new { phone, password })
        };
        var data = await SendAsync<VerificationPayload>(request, cancellationToken);

        return data switch
        {
            null => CredentialCheck.Unavailable(),
            { IsVerified: true, EntityId: { Length: > 0 } entityId } => CredentialCheck.Verified(entityId),
            _ => CredentialCheck.Rejected()
        };
    }

    public async Task<CredentialCheck> CheckEligibilityAsync(string entityId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{EligibilityPath}/{Uri.EscapeDataString(entityId)}");
        var data = await SendAsync<EligibilityPayload>(request, cancellationToken);

        return data switch
        {
            null => CredentialCheck.Unavailable(),
            { IsEligible: true } => CredentialCheck.Verified(entityId),
            _ => CredentialCheck.Rejected()
        };
    }

    private async Task<TPayload?> SendAsync<TPayload>(HttpRequestMessage request, CancellationToken cancellationToken)
        where TPayload : class
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", signer.IssueServiceToken());
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            return response.StatusCode == HttpStatusCode.OK ? await ReadAsync<TPayload>(response, cancellationToken) : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException
                                          && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static async Task<TPayload?> ReadAsync<TPayload>(HttpResponseMessage response, CancellationToken cancellationToken)
        where TPayload : class
    {
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<TPayload>>(Json, cancellationToken);

        return envelope?.Data;
    }

    private sealed record Envelope<TPayload>(TPayload? Data);

    private sealed record VerificationPayload(bool IsVerified, string? EntityId);

    private sealed record EligibilityPayload(bool IsEligible);
}

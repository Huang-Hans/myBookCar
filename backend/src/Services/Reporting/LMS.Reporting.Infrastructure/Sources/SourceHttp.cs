using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Infrastructure.ServiceIdentity;
using LMS.Reporting.Infrastructure.Sources.Payloads;

namespace LMS.Reporting.Infrastructure.Sources;

// 以 Reporting 服务身份读取报表来源：签名未配置、网络失败、超时、非成功响应或空数据，一律视为来源不可用。
internal static class SourceHttp
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<T> GetAsync<T>(HttpClient client, IServiceTokenIssuer tokens, string path, string source, CancellationToken cancellationToken)
        where T : class
    {
        if (!tokens.IsAvailable)
        {
            throw new ReportSourceUnavailableException(source);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Issue(ServiceScopes.ReportingSourceRead));
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var envelope = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<SourceEnvelope<T>>(Json, cancellationToken)
                : null;

            return envelope?.Data ?? throw new ReportSourceUnavailableException(source);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new ReportSourceUnavailableException(source, exception);
        }
    }

    public static string Instant(DateTimeOffset value) => Uri.EscapeDataString(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
}

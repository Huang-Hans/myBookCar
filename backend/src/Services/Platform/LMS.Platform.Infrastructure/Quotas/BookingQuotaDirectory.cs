using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Quotas;
using Microsoft.Extensions.Logging;

namespace LMS.Platform.Infrastructure.Quotas;

// 通过 Booking 的服务间端点读取权威每日限额，转发终端用户自己的令牌。任何失败都返回 null（按合同省略 dailyQuota），不影响配置读取。
internal sealed class BookingQuotaDirectory(HttpClient client, ICallerToken callerToken, ILogger<BookingQuotaDirectory> logger)
    : IPropertyQuotaDirectory
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyDictionary<string, int>?> GetDailyQuotasAsync(CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "internal/v1/property-quotas/daily-quotas");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", callerToken.Value);
            using var response = await client.SendAsync(request, cancellationToken);

            return response.StatusCode == HttpStatusCode.OK ? await ReadAsync(response, cancellationToken) : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The daily quotas of Booking could not be read; dailyQuota is omitted.");
            return null;
        }
    }

    private static async Task<IReadOnlyDictionary<string, int>?> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var envelope = await response.Content.ReadFromJsonAsync<DailyQuotaEnvelope>(Json, cancellationToken);

        return envelope?.Data?.ToDictionary(item => item.PropertyId, item => item.DailyQuota, StringComparer.Ordinal);
    }

    private sealed record DailyQuotaEnvelope(List<DailyQuotaItem>? Data);

    private sealed record DailyQuotaItem(string PropertyId, int DailyQuota);
}

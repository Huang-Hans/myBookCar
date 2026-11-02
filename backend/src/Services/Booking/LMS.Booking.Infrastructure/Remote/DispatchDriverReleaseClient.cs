using System.Globalization;
using System.Net.Http.Json;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Infrastructure.ServiceIdentity;
using Microsoft.Extensions.Logging;

namespace LMS.Booking.Infrastructure.Remote;

// 通过 Dispatch 的内部受限接口释放司机。使用 Booking 自己的服务身份令牌（只含司机释放范围），不转发终端用户令牌：
// 取消可能由客服或客户发起，他们的令牌都无权修改司机状态。尽力而为，任何失败只记录警告，不重试、不补发。
internal sealed class DispatchDriverReleaseClient(
    HttpClient client,
    IServiceTokenIssuer tokens,
    ILogger<DispatchDriverReleaseClient> logger) : IDispatchDriverReleases
{
    private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    public async Task ReleaseAsync(string driverId, string tripId, DateTimeOffset endedAt, CancellationToken cancellationToken)
    {
        if (!tokens.IsAvailable)
        {
            logger.LogWarning("Driver {DriverId} was not released for trip {TripId}: service signing is not configured.", driverId, tripId);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/v1/drivers/{Uri.EscapeDataString(driverId)}/availability-releases")
        {
            Content = JsonContent.Create(new { tripId, endedAt = endedAt.UtcDateTime.ToString(UtcFormat, CultureInfo.InvariantCulture) })
        };
        using var response = await RemoteHttp.SendAsync(client, request, tokens.Issue(ServiceScopes.DriverAvailabilityRelease), cancellationToken);
        if (response is null || !response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Driver {DriverId} was not released for trip {TripId}: Dispatch answered {Status}.",
                driverId, tripId, response is null ? "unavailable" : ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture));
        }
    }
}

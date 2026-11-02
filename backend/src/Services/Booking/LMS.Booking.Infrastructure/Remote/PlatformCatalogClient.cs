using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Directory;

namespace LMS.Booking.Infrastructure.Remote;

// Platform 上传配置的只读目录客户端：转发终端用户令牌调用 Platform 公共只读接口
// （locations/{id}、cancellation-reasons/{code}、properties/{id}）。
// 对方不可用或无法理解时按 SourceUnavailable 处理，不使用缓存或默认值。
internal sealed class PlatformCatalogClient(HttpClient client, ICallerToken callerToken) : IPlatformCatalog
{
    public async Task<DirectoryResult<CatalogLocation>> GetLocationAsync(string locationId, CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<CatalogLocation>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/platform/staff/v1/locations/{Uri.EscapeDataString(locationId)}");

        return await RemoteHttp.ReadAsync<RemoteLocationPayload, CatalogLocation>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            payload => new CatalogLocation(payload.LocationId, payload.Name, payload.Region, payload.CategoryCode, payload.IsActive),
            cancellationToken);
    }

    public async Task<DirectoryResult<CatalogCancellationReason>> GetCancellationReasonAsync(
        string reasonCode,
        CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<CatalogCancellationReason>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/platform/staff/v1/cancellation-reasons/{Uri.EscapeDataString(reasonCode)}");

        return await RemoteHttp.ReadAsync<RemoteCancellationReasonPayload, CatalogCancellationReason>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            payload => new CatalogCancellationReason(
                payload.ReasonCode, payload.Name, payload.PointsReturnPolicyCode, payload.PointsReturnReference),
            cancellationToken);
    }

    public async Task<DirectoryResult<CatalogProperty>> GetPropertyAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<CatalogProperty>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/platform/staff/v1/properties/{Uri.EscapeDataString(propertyId)}");

        return await RemoteHttp.ReadAsync<RemotePropertyPayload, CatalogProperty>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            payload => new CatalogProperty(payload.PropertyId, payload.Name, payload.CancellationPolicyReference),
            cancellationToken);
    }

    private sealed record RemoteLocationPayload(string LocationId, string Name, string Region, string CategoryCode, bool IsActive);

    private sealed record RemoteCancellationReasonPayload(
        string ReasonCode, string Name, string PointsReturnPolicyCode, string PointsReturnReference);

    private sealed record RemotePropertyPayload(string PropertyId, string Name, string CancellationPolicyReference);
}

using System.Net.Http.Json;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.SelfService.Ports;

namespace LMS.Booking.Infrastructure.Remote;

// 客户自助预订的 Platform 只读目录客户端：转发客户的令牌调用 Platform 专为已绑定客户开放的服务间目录端点（internal/v1/catalog）。
// 员工目录接口要求员工权限，客户本人没有；这里的端点只返回客户可见字段。
// 对方不可用或无法理解时按 Unavailable 处理，不使用缓存或默认值。
internal sealed class PlatformSelfCatalogClient(HttpClient client, ICallerToken callerToken) : IPlatformSelfCatalog
{
    private const string CatalogPath = "internal/v1/catalog";

    public Task<DirectoryResult<CatalogLocation>> GetLocationAsync(string locationId, CancellationToken cancellationToken) =>
        GetAsync<RemoteLocationPayload, CatalogLocation>(
            $"locations/{Uri.EscapeDataString(locationId)}",
            payload => new CatalogLocation(payload.LocationId, payload.Name, payload.Region, payload.CategoryCode, payload.IsActive),
            cancellationToken);

    public Task<DirectoryResult<CatalogLocationProfile>> GetLocationProfileAsync(string locationId, CancellationToken cancellationToken) =>
        GetAsync<RemoteLocationPayload, CatalogLocationProfile>(
            $"locations/{Uri.EscapeDataString(locationId)}",
            payload => new CatalogLocationProfile(
                payload.LocationId, payload.Name, payload.Address ?? string.Empty, payload.Region, payload.CategoryCode,
                payload.OperatingHours ?? string.Empty, payload.IsActive, ToCoordinates(payload.Coordinates)),
            cancellationToken);

    public async Task<DirectoryResult<CatalogLocationPage>> ListLocationsAsync(
        string? search,
        string? region,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<CatalogLocationPage>.Failed(DirectoryOutcome.Unavailable);
        }

        var query = $"page={page}&pageSize={pageSize}&sort=name";
        query += search is null ? string.Empty : $"&search={Uri.EscapeDataString(search)}";
        query += region is null ? string.Empty : $"&region={Uri.EscapeDataString(region)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{CatalogPath}/locations?{query}");

        return await RemoteHttp.ReadEnvelopeAsync<List<RemoteLocationPayload>, CatalogLocationPage>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            envelope => new CatalogLocationPage(
                envelope.Data!.Select(item => new CatalogLocationDetail(
                    item.LocationId, item.Name, item.Address ?? string.Empty, item.Region, item.CategoryCode, item.OperatingHours ?? string.Empty)).ToList(),
                envelope.Meta?.Pagination?.TotalItems ?? envelope.Data!.Count),
            cancellationToken);
    }

    public Task<DirectoryResult<CatalogProperty>> GetPropertyAsync(string propertyId, CancellationToken cancellationToken) =>
        GetAsync<RemotePropertyPayload, CatalogProperty>(
            $"properties/{Uri.EscapeDataString(propertyId)}",
            payload => new CatalogProperty(payload.PropertyId, payload.Name, payload.CancellationPolicyReference),
            cancellationToken);

    public Task<DirectoryResult<CatalogCancellationReason>> GetCancellationReasonAsync(string reasonCode, CancellationToken cancellationToken) =>
        GetAsync<RemoteCancellationReasonPayload, CatalogCancellationReason>(
            $"cancellation-reasons/{Uri.EscapeDataString(reasonCode)}",
            payload => new CatalogCancellationReason(payload.ReasonCode, payload.Name, payload.PointsReturnPolicyCode, payload.PointsReturnReference),
            cancellationToken);

    public Task<DirectoryResult<SelfBookingRules>> GetBookingRulesAsync(CancellationToken cancellationToken) =>
        GetAsync<RemoteBookingRulesPayload, SelfBookingRules>(
            "self-booking-rules",
            payload => new SelfBookingRules(payload.MinimumLeadTimeHours, payload.MaximumAdvanceDays, payload.MaximumActiveRequestsPerGuestPerDay),
            cancellationToken);

    public Task<DirectoryResult<SelfEligibilityRules>> GetEligibilityRulesAsync(CancellationToken cancellationToken) =>
        GetAsync<RemoteEligibilityRulesPayload, SelfEligibilityRules>(
            "self-booking-eligibility-rules",
            payload => new SelfEligibilityRules(payload.EligibleTierCodes),
            cancellationToken);

    public async Task<DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>> CheckRestrictionsAsync(
        IReadOnlyList<string> memberIds,
        CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{CatalogPath}/customer-restriction-checks")
        {
            Content = JsonContent.Create(new { memberIds })
        };

        return await RemoteHttp.ReadAsync<List<RemoteRestrictionPayload>, IReadOnlyList<CustomerRestrictionInfo>>(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken),
            payload => payload.Select(item => new CustomerRestrictionInfo(item.MemberId, item.Reason)).ToList(),
            cancellationToken);
    }

    private async Task<DirectoryResult<TValue>> GetAsync<TPayload, TValue>(
        string relativePath,
        Func<TPayload, TValue> map,
        CancellationToken cancellationToken)
    {
        if (callerToken.Value is null)
        {
            return DirectoryResult<TValue>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{CatalogPath}/{relativePath}");

        return await RemoteHttp.ReadAsync(
            await RemoteHttp.SendAsync(client, request, callerToken.Value, cancellationToken), map, cancellationToken);
    }

    // 缺失或数值越界的坐标按“没有坐标”处理，不修正、不猜测。
    private static CatalogCoordinates? ToCoordinates(RemoteCoordinatesPayload? payload) =>
        payload is null ? null : CatalogCoordinates.TryCreate(payload.Latitude, payload.Longitude);

    private sealed record RemoteLocationPayload(
        string LocationId, string Name, string? Address, string Region, string CategoryCode, string? OperatingHours, bool IsActive,
        RemoteCoordinatesPayload? Coordinates = null);

    private sealed record RemoteCoordinatesPayload(double Latitude, double Longitude);

    private sealed record RemotePropertyPayload(string PropertyId, string Name, string CancellationPolicyReference);

    private sealed record RemoteCancellationReasonPayload(
        string ReasonCode, string Name, string PointsReturnPolicyCode, string PointsReturnReference);

    private sealed record RemoteBookingRulesPayload(int MinimumLeadTimeHours, int MaximumAdvanceDays, int MaximumActiveRequestsPerGuestPerDay);

    private sealed record RemoteEligibilityRulesPayload(List<string> EligibleTierCodes);
}

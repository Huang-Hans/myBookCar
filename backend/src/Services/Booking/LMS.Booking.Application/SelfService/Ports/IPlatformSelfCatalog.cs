using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Directory;

namespace LMS.Booking.Application.SelfService.Ports;

// 客户自助预订所需的 Platform 只读目录。员工目录接口要求员工权限，客户本人没有，因此走 Platform 专为已绑定客户开放的服务间目录端点。
// 转发客户的令牌，Platform 重新检查其为已绑定的客户；对方不可用时按 Unavailable 处理，不使用缓存或默认值。
public interface IPlatformSelfCatalog
{
    Task<DirectoryResult<CatalogLocation>> GetLocationAsync(string locationId, CancellationToken cancellationToken);

    // 读取单个地点的客户可见信息与坐标（含已停用的地点，是否可见由调用方判断）。
    Task<DirectoryResult<CatalogLocationProfile>> GetLocationProfileAsync(string locationId, CancellationToken cancellationToken);

    Task<DirectoryResult<CatalogLocationPage>> ListLocationsAsync(
        string? search,
        string? region,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DirectoryResult<CatalogProperty>> GetPropertyAsync(string propertyId, CancellationToken cancellationToken);

    Task<DirectoryResult<CatalogCancellationReason>> GetCancellationReasonAsync(string reasonCode, CancellationToken cancellationToken);

    Task<DirectoryResult<SelfBookingRules>> GetBookingRulesAsync(CancellationToken cancellationToken);

    Task<DirectoryResult<SelfEligibilityRules>> GetEligibilityRulesAsync(CancellationToken cancellationToken);

    Task<DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>> CheckRestrictionsAsync(
        IReadOnlyList<string> memberIds,
        CancellationToken cancellationToken);
}

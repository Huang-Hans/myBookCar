using LMS.Booking.Application.Directory;

namespace LMS.Booking.Application.Catalog.Ports;

// Platform 上传配置的只读目录（地点/取消原因/物业政策），供控制台建单与取消校验使用。
// 调用转发终端用户令牌，因此调用者需持有对应只读权限（locations:read / configuration:read）。
public interface IPlatformCatalog
{
    Task<DirectoryResult<CatalogLocation>> GetLocationAsync(string locationId, CancellationToken cancellationToken);

    Task<DirectoryResult<CatalogCancellationReason>> GetCancellationReasonAsync(string reasonCode, CancellationToken cancellationToken);

    Task<DirectoryResult<CatalogProperty>> GetPropertyAsync(string propertyId, CancellationToken cancellationToken);
}

public sealed record CatalogLocation(string LocationId, string Name, string Region, string CategoryCode, bool IsActive);

public sealed record CatalogCancellationReason(string ReasonCode, string Name, string PointsReturnPolicyCode, string PointsReturnReference);

public sealed record CatalogProperty(string PropertyId, string Name, string CancellationPolicyReference);

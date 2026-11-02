using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.SelfService.Ports;

namespace LMS.Booking.Application.SelfService.Handlers;

public interface ICustomerLocationsHandler
{
    Task<OperationResult<PageResult<CatalogLocationDetail>>> ListAsync(string? region, ListQuery list, CancellationToken cancellationToken);

    // 按地点 ID 读取客户可见的地点详情（含坐标）。可见范围与列表一致：只有启用的目录地点；不存在或已停用都是 NotFound。
    Task<OperationResult<CatalogLocationProfile>> GetAsync(string locationId, CancellationToken cancellationToken);
}

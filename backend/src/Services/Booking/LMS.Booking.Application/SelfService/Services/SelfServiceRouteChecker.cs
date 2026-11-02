using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.Trips.Services;

namespace LMS.Booking.Application.SelfService.Services;

// 自助预订的路线检查：目录地点 ID 必须存在且启用（不接受任意文本冒充地点 ID），起终点不同，并且符合产品的路线范围
// （LOCAL 起终点同城；CROSS_BORDER 起终点分属澳门与香港）。目录 ID 与地址快照分开：快照来自目录地点的名称。
internal sealed class SelfServiceRouteChecker(IPlatformSelfCatalog catalog)
{
    public async Task<OperationResult<CheckedRoute>> CheckAsync(
        ServiceProductOption product,
        string pickupLocationId,
        string dropoffLocationId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(pickupLocationId, dropoffLocationId, StringComparison.Ordinal))
        {
            return OperationResult<CheckedRoute>.Invalid([new FieldIssue("dropoffLocationId", "Must differ from pickupLocationId")]);
        }

        var pickup = await catalog.GetLocationAsync(pickupLocationId, cancellationToken);
        var dropoff = await catalog.GetLocationAsync(dropoffLocationId, cancellationToken);
        return ServiceProductRouteRules.Check(product, pickup, dropoff);
    }
}

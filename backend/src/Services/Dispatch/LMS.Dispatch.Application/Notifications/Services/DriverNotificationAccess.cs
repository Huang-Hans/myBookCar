using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Notifications.Services;

// 司机是否仍可保有接单消息订阅：司机记录必须存在且为启用状态，停用 / 暂停的司机不继续接收。
// 工作状态（可接单 / 休息 / 执行中）不影响订阅：执行中的司机仍需收到接受成功等消息。
public sealed class DriverNotificationAccess(IDriverRepository drivers)
{
    public async Task<bool> IsUsableAsync(string driverId, CancellationToken cancellationToken) =>
        await drivers.FindByIdAsync(driverId, cancellationToken) is { Status: DriverStatus.Active };
}

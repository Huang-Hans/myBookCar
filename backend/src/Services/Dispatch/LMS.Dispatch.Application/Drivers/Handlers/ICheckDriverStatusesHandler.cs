using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Models;

namespace LMS.Dispatch.Application.Drivers.Handlers;

public interface ICheckDriverStatusesHandler
{
    // 只返回对调用者物业范围可见的司机；不可见或不存在的标识不出现在结果中。
    Task<IReadOnlyList<DriverStatusInfo>> HandleAsync(
        IReadOnlyList<string> driverIds,
        PropertyScope scope,
        CancellationToken cancellationToken);
}

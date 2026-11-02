using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

public interface IReleaseDriverHandler
{
    // 行程结束后释放司机：仍为 ON_TRIP 且该状态不晚于行程结束时写入时恢复为 AVAILABLE；否则保持不变并返回当前记录。
    Task<OperationResult<Driver>> HandleAsync(ReleaseDriverCommand command, CancellationToken cancellationToken);
}

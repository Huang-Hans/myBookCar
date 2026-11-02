using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Self.Handlers;

public interface IDriverSelfQueryHandler
{
    // 本人司机资料。ON_TRIP 状态会先与 Booking 对账；对账来源不可用时为 SourceUnavailable。
    Task<OperationResult<Driver>> GetAsync(string driverId, CommandContext context, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.TripLocations.Commands;

namespace LMS.Booking.Application.TripLocations.Handlers;

public interface IRecordTripLocationsHandler
{
    // 成功时返回服务端已保存的最大序号；不是本人行程为 NotFound，不在共享窗口为 StateConflict。
    Task<OperationResult<long>> HandleAsync(RecordTripLocationsCommand command, CancellationToken cancellationToken);
}

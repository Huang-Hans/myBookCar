using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Application.Shifts.Services;

namespace LMS.Dispatch.Application.Shifts.Handlers;

// 按请求中的司机标识查询其全部班次。司机不在调用者物业范围内时与不存在相同。
internal sealed class DriverShiftListQueryHandler(
    IDriverRepository drivers,
    IDriverShiftRepository shifts,
    ShiftCardComposer cards) : IDriverShiftListQueryHandler
{
    public async Task<OperationResult<PageResult<ShiftCard>>> ListAsync(
        string driverId,
        PropertyScope scope,
        ListQuery list,
        CancellationToken cancellationToken)
    {
        var driver = await drivers.FindVisibleAsync(driverId, scope, cancellationToken);
        if (driver is null)
        {
            return OperationResult<PageResult<ShiftCard>>.Failed(OperationOutcome.NotFound);
        }

        var page = await shifts.ListForDriverAsync(driverId, scope, list, cancellationToken);
        var composed = await cards.ComposeAsync(page.Items, cancellationToken);

        return OperationResult<PageResult<ShiftCard>>.Succeeded(new PageResult<ShiftCard>(composed, page.TotalItems));
    }
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.DriverTrips.Services;

// 行程进入终态（完成 / 取消）并已提交后，通知 Dispatch 释放执行该行程的司机。
// 只在本次请求实际改变了行程时通知：失败结果与幂等重放都不通知；没有指派司机的行程不需要释放。
internal sealed class TripDriverReleaseNotifier(IDispatchDriverReleases releases, TimeProvider timeProvider)
{
    public async Task<OperationResult<Trip>> AfterCommitAsync(OperationResult<Trip> result, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess || result.Outcome == OperationOutcome.Replayed)
        {
            return result;
        }

        var trip = result.Value!;
        if (trip.IsTerminal && trip.Assignment is { } assignment)
        {
            await releases.ReleaseAsync(assignment.DriverId, trip.TripId, trip.ClosedAt ?? timeProvider.GetUtcNow(), cancellationToken);
        }

        return result;
    }
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.TripLocations.Services;

// 行程进入终态（完成 / 取消）并已提交后，通知正在查看的客户与客服位置共享结束。
// 只在本次请求实际改变了行程时通知：失败结果与幂等重放都不通知。之后的上报会因不在共享窗口被拒绝；
// 最新位置记录不再有写入，按保留期自动删除。推送失败由发布实现吞掉，不影响已提交的结果。
internal sealed class TripLocationEndNotifier(ITripLocationPublisher publisher)
{
    public async Task<OperationResult<Trip>> AfterCommitAsync(OperationResult<Trip> result, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess || result.Outcome == OperationOutcome.Replayed || !result.Value!.IsTerminal)
        {
            return result;
        }

        await publisher.PublishEndedAsync(result.Value.TripId, cancellationToken);

        return result;
    }
}

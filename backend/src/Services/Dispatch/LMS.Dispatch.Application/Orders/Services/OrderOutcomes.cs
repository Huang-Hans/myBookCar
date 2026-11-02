using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Directory;

namespace LMS.Dispatch.Application.Orders.Services;

// 把对 Booking 的调用结果归入业务用例的结果分类：Booking 不可用是来源不可用，不能被当作空队列或订单不存在。
internal static class OrderOutcomes
{
    public static OperationOutcome Of(BookingOutcome outcome) => outcome switch
    {
        BookingOutcome.NotFound => OperationOutcome.NotFound,
        BookingOutcome.Forbidden => OperationOutcome.Forbidden,
        BookingOutcome.Conflict => OperationOutcome.StateConflict,
        _ => OperationOutcome.SourceUnavailable
    };
}

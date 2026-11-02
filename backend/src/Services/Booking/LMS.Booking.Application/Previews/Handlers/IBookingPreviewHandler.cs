using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Previews.Models;

namespace LMS.Booking.Application.Previews.Handlers;

public interface IBookingPreviewHandler
{
    // 宾客 / 地点不存在、越权或来源不可用按对应失败返回；业务规则不满足不是失败，而是 canSubmit=false 并给出原因。
    Task<OperationResult<BookingPreview>> HandleAsync(BookingPreviewQuery query, CancellationToken cancellationToken);
}

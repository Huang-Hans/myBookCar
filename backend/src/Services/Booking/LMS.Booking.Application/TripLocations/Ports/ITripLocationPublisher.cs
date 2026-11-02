using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Application.TripLocations.Ports;

// 向正在查看该行程的客户与客服推送位置。直接推送、不存库、不重试：位置只保留最新，旧点没有价值。
// 实现必须吞掉推送失败（只记日志），不能影响已经保存的位置或已经提交的行程结果。
public interface ITripLocationPublisher
{
    Task PublishUpdatedAsync(string tripId, LocationPoint point, CancellationToken cancellationToken);

    Task PublishEndedAsync(string tripId, CancellationToken cancellationToken);
}

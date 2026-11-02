namespace LMS.Dispatch.Application.Directory;

// Booking 拥有行程与权威指派；Dispatch 只通过它读取待分配行程、请求条件指派并核对司机的有效行程，不直接访问 Booking 的数据库。
public interface IBookingAssignments
{
    // 员工令牌：读取待分配行程摘要（创建邀请）。
    Task<BookingResult<AssignableTrip>> GetAssignableTripAsync(string tripId, CancellationToken cancellationToken);

    // 司机令牌：请求把行程指派给本人。以 OfferId 幂等；成功即表示 Booking 已权威提交。
    Task<BookingResult<ActiveTripReference>> AssignDriverAsync(AssignDriverRequest request, CancellationToken cancellationToken);

    // 司机令牌：读取本人当前有效行程；NotFound 表示没有。
    Task<BookingResult<ActiveTripReference>> GetActiveTripAsync(CancellationToken cancellationToken);

    // 司机令牌：核对指定行程是否归属本人（含已完成 / 已取消的行程）；NotFound 表示不是本人的行程或不存在。
    Task<BookingResult<ActiveTripReference>> GetDriverTripAsync(string tripId, CancellationToken cancellationToken);
}

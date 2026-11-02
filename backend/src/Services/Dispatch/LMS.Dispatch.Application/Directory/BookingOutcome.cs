namespace LMS.Dispatch.Application.Directory;

public enum BookingOutcome
{
    Success,
    NotFound,

    // 行程当前不允许该操作（已被指派、已取消、状态不符或司机另有执行中的行程）。
    Conflict,
    Forbidden,

    // Booking 暂时不可用或结果无法确定；可用同一操作标识重试。
    Unavailable
}

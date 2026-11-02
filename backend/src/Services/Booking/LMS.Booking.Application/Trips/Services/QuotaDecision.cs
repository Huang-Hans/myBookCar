namespace LMS.Booking.Application.Trips.Services;

// 物业当日配额判定：可受理（有余量，或耗尽但策略为 WAITLIST 不拦截）、耗尽后自动拒绝（AUTO_REJECT）。
internal enum QuotaDecision
{
    Available,
    Rejected
}

using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Quotas;

namespace LMS.Booking.Application.Trips.Services;

// 物业当日配额（D5）：已用量取本服务当日非取消行程数；未配置配额的物业不限制。客服代订与客户自助预订共用同一口径。
// 耗尽后只有 AUTO_REJECT 拒绝；WAITLIST 不拦截，订单照常确认，因此已用量可能超过配额。
internal sealed class PropertyQuotaGate(IPropertyQuotaRepository quotas, ITripRepository trips)
{
    public async Task<QuotaDecision> DecideAsync(string propertyId, DateTimeOffset pickupAt, CancellationToken cancellationToken)
    {
        var quota = await quotas.FindByIdAsync(propertyId, cancellationToken);
        if (quota is null)
        {
            return QuotaDecision.Available;
        }

        var (dayStart, dayEnd) = MacauClock.DayWindow(MacauClock.DateOf(pickupAt));
        var used = await trips.CountDailyActiveAsync(propertyId, dayStart, dayEnd, cancellationToken);
        if (used < quota.DailyQuota)
        {
            return QuotaDecision.Available;
        }

        return quota.ExhaustionPolicy == ExhaustionPolicy.AutoReject ? QuotaDecision.Rejected : QuotaDecision.Available;
    }
}

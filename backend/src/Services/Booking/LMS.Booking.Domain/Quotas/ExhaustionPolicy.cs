namespace LMS.Booking.Domain.Quotas;

// 物业限额耗尽后的处理。对外字面值为 WAITLIST / AUTO_REJECT。WAITLIST 不拦截、订单照常确认；AUTO_REJECT 拒绝下单。
public enum ExhaustionPolicy
{
    Waitlist,
    AutoReject
}

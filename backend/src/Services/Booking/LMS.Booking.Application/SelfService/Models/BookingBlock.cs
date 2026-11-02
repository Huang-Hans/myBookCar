namespace LMS.Booking.Application.SelfService.Models;

// 客户当前不能自助预订的原因。对外字面值为 PROPERTY_UNASSIGNED / TIER_NOT_ELIGIBLE / RESTRICTED。
public enum BookingBlock
{
    PropertyUnassigned,
    TierNotEligible,
    Restricted
}

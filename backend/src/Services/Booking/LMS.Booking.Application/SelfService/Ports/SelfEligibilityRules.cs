namespace LMS.Booking.Application.SelfService.Ports;

// Platform 自助资格说明（CF16）中可执行的部分：允许自助预订的会员等级代码。
public sealed record SelfEligibilityRules(IReadOnlyList<string> EligibleTierCodes);

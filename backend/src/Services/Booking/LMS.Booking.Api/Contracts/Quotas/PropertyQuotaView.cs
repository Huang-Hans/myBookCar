namespace LMS.Booking.Api.Contracts.Quotas;

/// <summary>
/// 物业限额配置：日/周/月额度与耗尽策略，以及只读的已用量。
/// </summary>
/// <param name="PropertyId">物业代码。</param>
/// <param name="DailyQuota">每日权益额度，非负。</param>
/// <param name="WeeklyQuota">每周额度，不得小于 dailyQuota。</param>
/// <param name="MonthlyQuota">每月额度，不得小于 weeklyQuota。</param>
/// <param name="ExhaustionPolicy">额度耗尽策略，取 WAITLIST 或 AUTO_REJECT。</param>
/// <param name="DailyUsed">当日只读已用量；未知时为空并省略该键，不用 0 冒充。</param>
/// <param name="WeeklyUsed">本周只读已用量；未知时为空并省略该键。</param>
/// <param name="MonthlyUsed">本月只读已用量；未知时为空并省略该键。</param>
/// <param name="Version">乐观锁版本号，配合 X-Expected-Version 头做并发控制。</param>
public sealed record PropertyQuotaView(
    string PropertyId,
    int DailyQuota,
    int WeeklyQuota,
    int MonthlyQuota,
    string ExhaustionPolicy,
    int? DailyUsed,
    int? WeeklyUsed,
    int? MonthlyUsed,
    long Version);

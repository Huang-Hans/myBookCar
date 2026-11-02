using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Api.Contracts.Quotas;

/// <summary>
/// 局部更新物业限额的请求体；至少出现一个字段，且合并后的日/周/月约束会与真实已用量一起校验。
/// </summary>
/// <param name="DailyQuota">每日权益额度，非负；键未出现表示保持原值，显式 null 不被接受。</param>
/// <param name="WeeklyQuota">每周额度，不得小于 dailyQuota；键未出现表示保持原值，显式 null 不被接受。</param>
/// <param name="MonthlyQuota">每月额度，不得小于 weeklyQuota；键未出现表示保持原值，显式 null 不被接受。</param>
/// <param name="ExhaustionPolicy">额度耗尽策略，取 WAITLIST 或 AUTO_REJECT；键未出现表示保持原值，显式 null 不被接受。</param>
public sealed record PatchPropertyQuotaRequest(
    Optional<int?> DailyQuota,
    Optional<int?> WeeklyQuota,
    Optional<int?> MonthlyQuota,
    Optional<string?> ExhaustionPolicy);

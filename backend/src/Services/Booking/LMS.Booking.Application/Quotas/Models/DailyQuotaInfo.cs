namespace LMS.Booking.Application.Quotas.Models;

// 物业的权威每日限额，供 Platform 在物业配置读取中组合（dailyQuota 只来自这里）。
public sealed record DailyQuotaInfo(string PropertyId, int DailyQuota);

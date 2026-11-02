namespace LMS.Booking.Api.Contracts.Quotas;

/// <summary>
/// 物业每日限额项；服务间接口使用，Platform 在读取物业配置时用它组合权威每日限额。
/// </summary>
/// <param name="PropertyId">物业代码。</param>
/// <param name="DailyQuota">该物业的每日权益额度，非负。</param>
public sealed record DailyQuotaView(string PropertyId, int DailyQuota);

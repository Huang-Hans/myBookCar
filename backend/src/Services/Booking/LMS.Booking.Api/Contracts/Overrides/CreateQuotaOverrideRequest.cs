namespace LMS.Booking.Api.Contracts.Overrides;

/// <summary>
/// 提交限额例外申请的请求体；不接受 requestedBy 或 status，也不能直接修改配额。
/// </summary>
/// <param name="PropertyId">适用物业代码，必填非空白。</param>
/// <param name="GuestId">适用客人标识，必填非空白。</param>
/// <param name="AdditionalDailyEntitlements">追加的每日权益额度，取值为 1 至 20。</param>
/// <param name="Justification">申请说明，必填非空白。</param>
public sealed record CreateQuotaOverrideRequest(string? PropertyId, string? GuestId, int? AdditionalDailyEntitlements, string? Justification);

namespace LMS.Booking.Api.Contracts.Overrides;

/// <summary>
/// 限额例外申请的当前状态；已决定的记录再次决定返回 409 / 40900。
/// </summary>
/// <param name="OverrideId">限额例外申请标识。</param>
/// <param name="PropertyId">适用物业代码。</param>
/// <param name="GuestId">适用客人标识。</param>
/// <param name="AdditionalDailyEntitlements">追加的每日权益额度，取值为 1 至 20。</param>
/// <param name="Justification">申请说明，必填非空白。</param>
/// <param name="Status">申请状态，取 MANAGER_APPROVAL_REQUIRED、APPROVED 或 REJECTED。</param>
/// <param name="RequestedBy">申请人，由服务端按认证身份记录，不接收请求伪造。</param>
/// <param name="RequestedAt">申请时间，UTC 时间点。</param>
/// <param name="Decision">经理最终决定；尚未决定时为空并省略该键。</param>
/// <param name="Version">乐观锁版本号，配合 X-Expected-Version 头做并发控制。</param>
public sealed record QuotaOverrideView(
    string OverrideId,
    string PropertyId,
    string GuestId,
    int AdditionalDailyEntitlements,
    string Justification,
    string Status,
    string RequestedBy,
    DateTime RequestedAt,
    OverrideDecisionView? Decision,
    long Version);

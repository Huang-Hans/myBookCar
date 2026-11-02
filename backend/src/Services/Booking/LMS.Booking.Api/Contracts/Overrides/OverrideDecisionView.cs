namespace LMS.Booking.Api.Contracts.Overrides;

/// <summary>
/// 限额例外的经理最终决定；尚未决定时该字段为空并在响应中省略。
/// </summary>
/// <param name="Decision">最终决定，APPROVED 或 REJECTED。</param>
/// <param name="DecidedBy">真实审核主体，由服务端按认证身份记录，不接收请求伪造。</param>
/// <param name="DecidedAt">决定时间，UTC 时间点。</param>
public sealed record OverrideDecisionView(string Decision, string DecidedBy, DateTime DecidedAt);

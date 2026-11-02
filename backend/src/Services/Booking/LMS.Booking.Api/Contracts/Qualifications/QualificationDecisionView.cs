namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 经理的决定（仅 APPROVED / REJECTED 记录有）。批准或拒绝只记录复审结果，不修改外部会员、积分或黑名单，也不默认绕过最低等级。
/// </summary>
/// <param name="DecidedBy">决定人用户标识。</param>
/// <param name="DecidedAt">决定时间（UTC）。</param>
/// <param name="Comments">决定说明。</param>
public sealed record QualificationDecisionView(string DecidedBy, DateTime DecidedAt, string? Comments);

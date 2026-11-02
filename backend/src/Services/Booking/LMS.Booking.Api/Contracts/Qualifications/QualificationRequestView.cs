namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 经理复审的申请快照（谁、何时、为什么提出）。不随批准或拒绝而改变。
/// </summary>
/// <param name="RequestedBy">申请人用户标识。</param>
/// <param name="RequestedByName">申请人姓名快照；存量记录省略。</param>
/// <param name="RequestedByRole">申请人角色代码；存量记录省略。</param>
/// <param name="Reason">申请理由。</param>
/// <param name="RequestedAt">申请时间（UTC）。</param>
public sealed record QualificationRequestView(
    string RequestedBy,
    string? RequestedByName,
    string? RequestedByRole,
    string? Reason,
    DateTime RequestedAt);

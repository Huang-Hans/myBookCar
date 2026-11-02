namespace LMS.Booking.Domain.Qualifications;

// 经理复审的申请快照：谁在什么时候出于什么理由发起。它不随后续批准 / 拒绝而被覆盖（那些写入 Justification / OperatorId / OperatedAt）。
// 早于该字段的存量记录在被决定时由最近一次升级的操作人回填；姓名与角色仅在新申请里保存。
public sealed record QualificationRequest(
    string RequestedBy,
    string? RequestedByName,
    string? RequestedByRole,
    string? Reason,
    DateTimeOffset RequestedAt);

using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Domain.Auditing;

// 资格操作审计的结构化上下文，不靠解析 changeSummary 文本。Status 只会是 ESCALATED 或 APPROVED。
public sealed record QualificationAuditContext(string GuestId, string Justification, QualificationStatus Status);

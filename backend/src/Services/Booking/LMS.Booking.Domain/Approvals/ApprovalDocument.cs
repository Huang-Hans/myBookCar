namespace LMS.Booking.Domain.Approvals;

// 一个材料槽。IsVerified 是管理端人工核验结果，与文件的安全状态相互独立。
public sealed record ApprovalDocument(ApprovalDocumentType DocumentType, bool IsVerified, string? FileId);

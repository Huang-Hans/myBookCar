using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Directory;

// Platform 登记的文件元数据中 Booking 关心的部分：归属的审核、材料类型和安全状态。
public sealed record RegisteredFile(string FileId, string? ApprovalId, ApprovalDocumentType? DocumentType, FileScanStatus ScanStatus);

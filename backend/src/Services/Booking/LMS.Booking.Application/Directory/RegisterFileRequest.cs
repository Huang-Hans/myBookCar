using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Directory;

public sealed record RegisterFileRequest(
    string ApprovalId,
    ApprovalDocumentType DocumentType,
    string PropertyId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256);

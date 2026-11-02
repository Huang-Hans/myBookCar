using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Commands;

public sealed record RegisterApprovalUploadCommand(
    string ApprovalId,
    ApprovalDocumentType DocumentType,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string IdempotencyKey,
    CommandContext Context);

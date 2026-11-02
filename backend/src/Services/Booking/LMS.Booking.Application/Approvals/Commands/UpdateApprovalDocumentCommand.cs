using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Commands;

public sealed record UpdateApprovalDocumentCommand(
    string ApprovalId,
    ApprovalDocumentType DocumentType,
    long ExpectedVersion,
    ApprovalDocumentChanges Changes,
    CommandContext Context);

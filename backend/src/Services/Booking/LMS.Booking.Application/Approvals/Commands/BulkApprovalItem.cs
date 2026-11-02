namespace LMS.Booking.Application.Approvals.Commands;

public sealed record BulkApprovalItem(string ApprovalId, long ExpectedVersion);

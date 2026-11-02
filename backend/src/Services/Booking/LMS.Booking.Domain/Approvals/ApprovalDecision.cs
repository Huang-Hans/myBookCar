using LMS.Booking.Domain.Common;

namespace LMS.Booking.Domain.Approvals;

public sealed record ApprovalDecision(ReviewDecision Decision, string Comments, string DecidedBy, DateTimeOffset DecidedAt);

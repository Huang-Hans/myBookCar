using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Models;

public sealed record ApprovalDecisionOutcome(string ApprovalId, ApprovalStatus Status, long Version);

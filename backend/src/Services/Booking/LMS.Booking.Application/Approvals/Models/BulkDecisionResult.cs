using LMS.Booking.Domain.Common;

namespace LMS.Booking.Application.Approvals.Models;

public sealed record BulkDecisionResult(IReadOnlyList<ApprovalDecisionOutcome> Items, ReviewDecision Decision);

using LMS.Booking.Domain.Common;

namespace LMS.Booking.Domain.Overrides;

public sealed record OverrideDecision(ReviewDecision Decision, string DecidedBy, DateTimeOffset DecidedAt);

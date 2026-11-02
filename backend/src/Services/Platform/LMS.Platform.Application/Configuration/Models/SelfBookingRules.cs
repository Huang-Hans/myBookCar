namespace LMS.Platform.Application.Configuration.Models;

public sealed record SelfBookingRules(
    int MinimumLeadTimeHours,
    int MaximumAdvanceDays,
    int MaximumActiveRequestsPerGuestPerDay,
    string PropertyAccessRule,
    string EligibilitySource);

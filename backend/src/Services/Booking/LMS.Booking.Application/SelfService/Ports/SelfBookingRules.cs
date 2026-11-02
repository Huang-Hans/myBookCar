namespace LMS.Booking.Application.SelfService.Ports;

// Platform 自助预约规则（CF15）。
public sealed record SelfBookingRules(int MinimumLeadTimeHours, int MaximumAdvanceDays, int MaximumActiveRequestsPerGuestPerDay);

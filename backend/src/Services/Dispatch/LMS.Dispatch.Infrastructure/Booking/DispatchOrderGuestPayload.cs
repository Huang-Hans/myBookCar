namespace LMS.Dispatch.Infrastructure.Booking;

internal sealed record DispatchOrderGuestPayload(string Name, string? RoomNumber, string TierCode, string? MemberId);

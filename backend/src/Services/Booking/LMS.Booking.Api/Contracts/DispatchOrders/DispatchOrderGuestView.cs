namespace LMS.Booking.Api.Contracts.DispatchOrders;

/// <summary>派单队列中的宾客摘要。</summary>
public sealed record DispatchOrderGuestView(string Name, string? RoomNumber, string TierCode, string? MemberId);

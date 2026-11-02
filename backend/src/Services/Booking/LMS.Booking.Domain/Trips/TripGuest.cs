namespace LMS.Booking.Domain.Trips;

// 下单时冻结的宾客快照。电话只保存掩码值，完整号码不出现在行程单中（隐私基线 D10）。
public sealed record TripGuest(
    string GuestId,
    string Name,
    string TierCode,
    string? RoomNumber,
    string? MemberId,
    string? PhoneMasked);

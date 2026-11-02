namespace LMS.Booking.Domain.Qualifications;

// 外部客人与等级快照。命名空间待确认（Q03）；不在本地重新评定等级。
public sealed record GuestSnapshot(string GuestId, string Name, string TierCode);

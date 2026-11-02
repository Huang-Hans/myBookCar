namespace LMS.Booking.Domain.Guests;

// 宾客读模型（一期 reference，实施基线）：来自 PMS/忠诚度快照，Booking 不拥有该主数据。
// 电话明文入库、出参一律掩码（D10）；游玩核验为快照布尔，不在此重新评定。
public sealed record GuestProfile(
    string GuestId,
    string MemberId,
    string Name,
    string TierCode,
    string? RoomNumber,
    string? PropertyId,
    DateOnly? StayFrom,
    DateOnly? StayTo,
    long? PointsBalance,
    string? Phone,
    bool InHouse,
    bool PlayVerified,
    int PlayWindowDays,
    string? PreferredLanguage,
    string? PreferredVehicleClass,
    string? ConfirmationChannel,
    string? SpecialRequests)
{
    public string MemberIdNormalized => MemberId.ToLowerInvariant();

    public string? MaskedPhone => PhoneMask.Mask(Phone);
}

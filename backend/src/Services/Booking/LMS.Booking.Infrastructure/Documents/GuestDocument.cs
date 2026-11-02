using LMS.Booking.Domain.Guests;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

// 宾客读模型（一期 reference）。日期为日历日（yyyy-MM-dd），非时刻；电话明文入库、出参掩码。
internal sealed class GuestDocument
{
    [BsonId]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("memberId")]
    public string MemberId { get; init; } = string.Empty;

    [BsonElement("memberIdNormalized")]
    public string MemberIdNormalized { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("tierCode")]
    public string TierCode { get; init; } = string.Empty;

    [BsonElement("roomNumber")]
    public string? RoomNumber { get; init; }

    [BsonElement("propertyId")]
    public string? PropertyId { get; init; }

    [BsonElement("stayFrom")]
    public string? StayFrom { get; init; }

    [BsonElement("stayTo")]
    public string? StayTo { get; init; }

    [BsonElement("pointsBalance")]
    public long? PointsBalance { get; init; }

    [BsonElement("phone")]
    public string? Phone { get; init; }

    [BsonElement("inHouse")]
    public bool InHouse { get; init; }

    [BsonElement("playVerified")]
    public bool PlayVerified { get; init; } = true;

    [BsonElement("playWindowDays")]
    public int PlayWindowDays { get; init; } = 90;

    [BsonElement("preferredLanguage")]
    public string? PreferredLanguage { get; init; }

    [BsonElement("preferredVehicleClass")]
    public string? PreferredVehicleClass { get; init; }

    [BsonElement("confirmationChannel")]
    public string? ConfirmationChannel { get; init; }

    [BsonElement("specialRequests")]
    public string? SpecialRequests { get; init; }

    // 本地登录凭据：带算法参数与盐的密码哈希，可空，缺字段按 null 读取。不来自 PMS/忠诚度快照，也不属于宾客领域模型，
    // 不进入任何 API 响应；快照导入整体替换文档时由 MongoGuestDirectory 保留原值，因此 FromDomain 不接触它。
    [BsonElement("password")]
    [BsonIgnoreIfNull]
    public string? Password { get; set; }

    public static GuestDocument FromDomain(GuestProfile guest) => new()
    {
        GuestId = guest.GuestId,
        MemberId = guest.MemberId,
        MemberIdNormalized = guest.MemberIdNormalized,
        Name = guest.Name,
        TierCode = guest.TierCode,
        RoomNumber = guest.RoomNumber,
        PropertyId = guest.PropertyId,
        StayFrom = guest.StayFrom is null ? null : StoredValues.FromDate(guest.StayFrom.Value),
        StayTo = guest.StayTo is null ? null : StoredValues.FromDate(guest.StayTo.Value),
        PointsBalance = guest.PointsBalance,
        Phone = guest.Phone,
        InHouse = guest.InHouse,
        PlayVerified = guest.PlayVerified,
        PlayWindowDays = guest.PlayWindowDays,
        PreferredLanguage = guest.PreferredLanguage,
        PreferredVehicleClass = guest.PreferredVehicleClass,
        ConfirmationChannel = guest.ConfirmationChannel,
        SpecialRequests = guest.SpecialRequests
    };

    public GuestProfile ToDomain() => new(
        GuestId,
        MemberId,
        Name,
        TierCode,
        RoomNumber,
        PropertyId,
        StayFrom is null ? null : StoredValues.ToDate(StayFrom, "stayFrom"),
        StayTo is null ? null : StoredValues.ToDate(StayTo, "stayTo"),
        PointsBalance,
        Phone,
        InHouse,
        PlayVerified,
        PlayWindowDays,
        PreferredLanguage,
        PreferredVehicleClass,
        ConfirmationChannel,
        SpecialRequests);
}

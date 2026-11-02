namespace LMS.Dispatch.Domain.Offers;

// 邀请创建时从 Booking 读取的行程摘要快照，供司机在响应前判断；权威行程以 Booking 为准。
public sealed record OfferTripSummary(
    DateTimeOffset PickupAt,
    string PickupName,
    string DropoffName,
    int Passengers,
    string VehicleClass);

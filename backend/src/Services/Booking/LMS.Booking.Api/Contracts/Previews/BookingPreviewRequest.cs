namespace LMS.Booking.Api.Contracts.Previews;

/// <summary>
/// 预订预览的请求体：创建代订行程输入的必要子集，字段含义与创建订单一致。预览没有任何副作用。
/// </summary>
/// <param name="GuestId">宾客标识。</param>
/// <param name="PropertyId">物业代码，必须在调用者授权范围内。</param>
/// <param name="PickupLocationId">取车地点（目录地点标识）。</param>
/// <param name="DropoffLocationId">送车地点（目录地点标识）。</param>
/// <param name="PickupAt">接车时间（ISO 8601 UTC，Z 结尾）；pickupMode 为 ASAP 时省略。</param>
/// <param name="PickupMode">ASAP / TODAY / SCHEDULED。</param>
/// <param name="ServiceType">ONE_WAY / AIRPORT_PICKUP / AIRPORT_DROPOFF / INTER_PROPERTY。</param>
/// <param name="VehicleClass">Optional when productCode is supplied; derived from product configuration. An explicit mismatch returns 40001. Required for legacy staff requests without productCode.</param>
/// <param name="Passengers">乘客数。</param>
/// <param name="Luggage">行李数。</param>
public sealed record BookingPreviewRequest(
    string? GuestId,
    string? PropertyId,
    string? PickupLocationId,
    string? DropoffLocationId,
    DateTimeOffset? PickupAt,
    string? PickupMode,
    string? ServiceType,
    string? VehicleClass,
    int? Passengers,
    int? Luggage,
    string? ProductCode = null);

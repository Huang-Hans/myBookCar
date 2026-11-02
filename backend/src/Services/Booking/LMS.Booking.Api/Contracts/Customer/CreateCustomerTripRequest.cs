namespace LMS.Booking.Api.Contracts.Customer;

/// <summary>
/// 客户自助建单的请求体。需要 Idempotency-Key 头；客户身份取自令牌，不接受任何宾客或经办人字段。
/// </summary>
/// <param name="ProductCode">服务产品：BUSINESS_SEDAN / LIMOUSINE / PARTY / CROSS_BORDER_TRANSFER，来自服务目录。</param>
/// <param name="PickupLocationId">上车地点（Platform 目录地点标识）；不接受任意文本冒充地点 ID。</param>
/// <param name="DropoffLocationId">下车地点（Platform 目录地点标识）。</param>
/// <param name="PickupMode">ASAP 或 SCHEDULED；ASAP 是否可用见服务目录的 isAsapAvailable。</param>
/// <param name="PickupAt">接车时间（ISO 8601 UTC，Z 结尾）；pickupMode 为 ASAP 时省略。须满足自助预约规则的最短提前量与最远预订天数。</param>
/// <param name="VehicleClass">Optional when productCode is supplied; derived from product configuration. An explicit mismatch returns 40001. Required for legacy staff requests without productCode.</param>
/// <param name="Passengers">乘客数，不得超过车型容量。</param>
/// <param name="Luggage">行李数；原型不收集，省略时记为 0（未申报），不得超过车型容量。</param>
/// <param name="SpecialRequests">订单备注，最长 500 字符。</param>
/// <param name="ConfirmationLanguage">确认语言，默认 EN。</param>
public sealed record CreateCustomerTripRequest(
    string? ProductCode,
    string? PickupLocationId,
    string? DropoffLocationId,
    string? PickupMode,
    DateTimeOffset? PickupAt,
    string? VehicleClass,
    int? Passengers,
    int? Luggage,
    string? SpecialRequests,
    string? ConfirmationLanguage);

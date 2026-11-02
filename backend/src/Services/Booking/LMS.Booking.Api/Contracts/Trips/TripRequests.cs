using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Api.Contracts.Trips;

/// <summary>
/// 创建代订行程的请求体。需要 Idempotency-Key 头；bookedBy 取自令牌，不接受任何经办人字段。
/// </summary>
/// <param name="GuestId">宾客标识（宾客检索/档案接口返回的 guestId）。</param>
/// <param name="PropertyId">物业代码，必须在调用者授权范围内。</param>
/// <param name="PickupLocationId">取车地点（Platform 上传地点）。</param>
/// <param name="DropoffLocationId">送车地点。</param>
/// <param name="PickupAt">接车时间（ISO 8601 UTC，Z 结尾）；pickupMode 为 ASAP 时省略。</param>
/// <param name="PickupMode">ASAP / TODAY / SCHEDULED。</param>
/// <param name="ServiceType">ONE_WAY / AIRPORT_PICKUP / AIRPORT_DROPOFF / INTER_PROPERTY。</param>
/// <param name="VehicleClass">Optional when productCode is supplied; derived from product configuration. An explicit mismatch returns 40001. Required for legacy staff requests without productCode.</param>
/// <param name="Passengers">乘客数，不得超过车型容量。</param>
/// <param name="Luggage">行李数，不得超过车型容量。</param>
/// <param name="SpecialRequests">可选特殊要求。</param>
/// <param name="ConfirmationLanguage">宾客确认语言，默认 EN。</param>
/// <param name="CouponId">優惠券標識。傳入後這張訂單使用一次該券；取消訂單時退還。省略表示不用券。</param>
public sealed record CreateTripRequest(
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
    string? SpecialRequests,
    string? ConfirmationLanguage,
    string? CouponId = null,
    string? ProductCode = null);

/// <summary>
/// 局部更新行程的请求体；X-Expected-Version 头可选且目前不参与比较。仅 PENDING~DRIVER_ASSIGNED 可修改。
/// modificationReason（GUEST_REQUEST / SCHEDULE_CHANGE）与 modificationComments 是本次修改的原因记录，可选，不是行程字段。
/// </summary>
public sealed record PatchTripRequest(
    Optional<string?> PickupLocationId,
    Optional<string?> DropoffLocationId,
    Optional<DateTimeOffset?> PickupAt,
    Optional<string?> PickupMode,
    Optional<string?> ServiceType,
    Optional<string?> VehicleClass,
    Optional<int?> Passengers,
    Optional<int?> Luggage,
    Optional<string?> SpecialRequests,
    Optional<string?> ConfirmationLanguage,
    string? ModificationReason = null,
    string? ModificationComments = null);

/// <summary>
/// 取消行程的请求体；需要 Idempotency-Key 头，X-Expected-Version 可选。reasonCode 必须来自上传取消原因清单；comments 可选，省略时记为空。
/// </summary>
public sealed record CancelTripRequest(string? ReasonCode, string? Comments);

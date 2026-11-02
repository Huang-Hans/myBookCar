namespace LMS.Dispatch.Api.Contracts.Coupons;

/// <summary>
/// 優惠券。狀態按澳門業務日、停用標記與已使用次數推導。
/// </summary>
/// <param name="CouponId">優惠券標識。</param>
/// <param name="Code">券碼。</param>
/// <param name="Name">名稱。</param>
/// <param name="MemberId">綁定的會員編號。</param>
/// <param name="Type">FREE_TRIP / DISCOUNT / FARE_DEDUCTION。</param>
/// <param name="FaceAmount">面額。含義見類型：行程次數、折扣百分比或澳門元。</param>
/// <param name="Scope">ALL_PROPERTIES / PROPERTY / VEHICLE。</param>
/// <param name="PropertyId">單一物業時的物業代碼；其他範圍省略。</param>
/// <param name="VehicleRestriction">限定車型，目前為 MAYBACH；其他範圍省略。</param>
/// <param name="ValidFrom">生效日，YYYY-MM-DD。</param>
/// <param name="ValidTo">截止日，YYYY-MM-DD。</param>
/// <param name="IssuedQuantity">發行量。</param>
/// <param name="RedeemedCount">已使用次數。</param>
/// <param name="Status">ACTIVE / UPCOMING / EXHAUSTED / DISABLED / EXPIRED，對應生效中、即將生效、已兌滿、停用、已過期。</param>
/// <param name="Version">版本。</param>
public sealed record CouponView(
    string CouponId,
    string Code,
    string Name,
    string MemberId,
    string Type,
    int FaceAmount,
    string Scope,
    string? PropertyId,
    string? VehicleRestriction,
    string ValidFrom,
    string ValidTo,
    int IssuedQuantity,
    int RedeemedCount,
    string Status,
    long Version);

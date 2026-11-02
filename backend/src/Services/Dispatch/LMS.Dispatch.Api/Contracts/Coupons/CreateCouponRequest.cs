namespace LMS.Dispatch.Api.Contracts.Coupons;

/// <summary>
/// 新增優惠券。必須綁定一名會員。
/// </summary>
/// <param name="Code">券碼。全庫不區分大小寫唯一，例如 WELCOME100。</param>
/// <param name="Name">名稱，例如迎賓旅途。</param>
/// <param name="MemberId">會員編號。這張券只屬於該會員。</param>
/// <param name="Type">類型字面值，取 FREE_TRIP / DISCOUNT / FARE_DEDUCTION，對應免費行程、折扣、車資抵扣。</param>
/// <param name="FaceAmount">面額。免費行程為行程次數；折扣為 1 到 100 的百分比；車資抵扣為澳門元正整數。</param>
/// <param name="Scope">適用範圍，取 ALL_PROPERTIES / PROPERTY / VEHICLE，對應所有物業、單一物業、限定車型。</param>
/// <param name="PropertyId">物業代碼。scope 為 PROPERTY 時必填，且必須在呼叫者授權範圍內；其他範圍必須省略。</param>
/// <param name="VehicleRestriction">限定車型，目前取 MAYBACH（僅限邁巴赫）。scope 為 VEHICLE 時必填；其他範圍必須省略。</param>
/// <param name="ValidFrom">生效日，YYYY-MM-DD。</param>
/// <param name="ValidTo">截止日，YYYY-MM-DD，不能早於生效日。</param>
/// <param name="IssuedQuantity">發行量，正整數。已使用次數從 0 起算。</param>
/// <param name="Disabled">是否停用。省略時為 false。停用後狀態為 DISABLED，不再按有效期顯示為生效中。</param>
public sealed record CreateCouponRequest(
    string? Code,
    string? Name,
    string? MemberId,
    string? Type,
    int? FaceAmount,
    string? Scope,
    string? PropertyId,
    string? VehicleRestriction,
    string? ValidFrom,
    string? ValidTo,
    int? IssuedQuantity,
    bool? Disabled);

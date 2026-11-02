using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Api.Contracts.Coupons;

/// <summary>
/// 部分更新優惠券。至少出現一個欄位。未傳的欄位保持原值。已使用次數不能在此修改。
/// </summary>
/// <param name="Code">券碼。改成其他已存在的券碼會被拒絕。</param>
/// <param name="Name">名稱。</param>
/// <param name="MemberId">會員編號。傳入後改綁到該會員。</param>
/// <param name="Type">FREE_TRIP / DISCOUNT / FARE_DEDUCTION。</param>
/// <param name="FaceAmount">面額。含義隨類型：行程次數、1 到 100 的百分比，或澳門元正整數。</param>
/// <param name="Scope">ALL_PROPERTIES / PROPERTY / VEHICLE。改範圍時，不再適用的物業或車型會被清掉。</param>
/// <param name="PropertyId">物業代碼。scope 為 PROPERTY 時必須有值；其他範圍應省略或傳 null。</param>
/// <param name="VehicleRestriction">限定車型，目前取 MAYBACH。scope 為 VEHICLE 時必須有值；其他範圍應省略或傳 null。</param>
/// <param name="ValidFrom">生效日，YYYY-MM-DD。</param>
/// <param name="ValidTo">截止日，YYYY-MM-DD，不能早於生效日。</param>
/// <param name="IssuedQuantity">發行量，正整數，不能小於已使用次數。</param>
/// <param name="Disabled">true 為停用，false 為恢復。停用後狀態為 DISABLED。</param>
public sealed record PatchCouponRequest(
    Optional<string?> Code,
    Optional<string?> Name,
    Optional<string?> MemberId,
    Optional<string?> Type,
    Optional<int?> FaceAmount,
    Optional<string?> Scope,
    Optional<string?> PropertyId,
    Optional<string?> VehicleRestriction,
    Optional<string?> ValidFrom,
    Optional<string?> ValidTo,
    Optional<int?> IssuedQuantity,
    Optional<bool?> Disabled);

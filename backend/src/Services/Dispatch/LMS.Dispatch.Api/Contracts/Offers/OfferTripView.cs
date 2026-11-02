namespace LMS.Dispatch.Api.Contracts.Offers;

/// <summary>
/// 邀请中的行程摘要（创建邀请时的快照）；权威行程信息以接单后的行程详情为准。
/// </summary>
/// <param name="PickupAt">计划接车时间（UTC）。</param>
/// <param name="PickupName">上车地点名称。</param>
/// <param name="DropoffName">下车地点名称。</param>
/// <param name="Passengers">乘客人数。</param>
/// <param name="VehicleClass">车型要求：BUSINESS_SEDAN / LUXURY_SEDAN / LUXURY_MPV / MAYBACH。</param>
public sealed record OfferTripView(DateTimeOffset PickupAt, string PickupName, string DropoffName, int Passengers, string VehicleClass);

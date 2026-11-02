using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>
/// 修改客户偏好的请求体（PATCH，至少一个字段）。可选 X-Expected-Version：提供时必须等于当前版本（从未保存过为 0），否则 409。
/// </summary>
/// <param name="Language">EN / ZH_HANT / ZH_HANS。</param>
/// <param name="IsPushNotificationsEnabled">推送通知总开关。</param>
public sealed record UpdateCustomerPreferencesRequest(Optional<string?> Language, Optional<bool?> IsPushNotificationsEnabled);

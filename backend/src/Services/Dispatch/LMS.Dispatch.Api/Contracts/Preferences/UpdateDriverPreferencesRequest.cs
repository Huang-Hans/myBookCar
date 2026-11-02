namespace LMS.Dispatch.Api.Contracts.Preferences;

/// <summary>
/// 修改司机偏好的请求体（PATCH，目前只有语言）。可选 X-Expected-Version：提供时必须等于当前版本（未保存过为 0 不可提供大于 0 的值），否则 409。
/// </summary>
/// <param name="Language">界面语言：EN / ZH_HANT / ZH_HANS。</param>
public sealed record UpdateDriverPreferencesRequest(string? Language);

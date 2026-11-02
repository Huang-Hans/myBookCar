namespace LMS.Dispatch.Api.Contracts.Drivers;

/// <summary>
/// 紧急联系人；新增司机请求体内必填。
/// </summary>
/// <param name="Name">紧急联系人姓名；必填，非空白。</param>
/// <param name="Phone">紧急联系人电话；必填，非空白。</param>
public sealed record EmergencyContactRequest(string? Name, string? Phone);

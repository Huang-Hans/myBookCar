namespace LMS.Dispatch.Api.Contracts.Drivers;

/// <summary>
/// 紧急联系人响应体；字段非空，总会输出。
/// </summary>
/// <param name="Name">紧急联系人姓名。</param>
/// <param name="Phone">紧急联系人电话。</param>
public sealed record EmergencyContactView(string Name, string Phone);

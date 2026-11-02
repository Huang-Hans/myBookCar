namespace LMS.Dispatch.Api.Contracts.Self;

/// <summary>
/// 切换工作状态的请求体。ON_TRIP 由有效指派驱动，不能由司机自报。
/// </summary>
/// <param name="Availability">目标工作状态：AVAILABLE / BREAK / OFF_DUTY。</param>
public sealed record ChangeAvailabilityRequest(string? Availability);

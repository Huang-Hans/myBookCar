namespace LMS.Dispatch.Api.Contracts.Internal;

/// <summary>
/// 服务间契约：司机状态核对结果的一条记录；只包含调用者物业范围内可见的司机。
/// </summary>
/// <param name="DriverId">司机标识。</param>
/// <param name="PropertyId">司机所属单一物业代码，用于调用者按其授权范围判断结果适用范围。</param>
/// <param name="Status">司机状态字面值，取 ACTIVE / INACTIVE / SUSPENDED。</param>
/// <param name="RestrictionReason">司机黑名单/停用限制的原因；无限制时为 null。</param>
public sealed record DriverStatusView(string DriverId, string PropertyId, string Status, string? RestrictionReason);

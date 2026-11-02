namespace LMS.Dispatch.Api.Contracts.Reports;

/// <summary>
/// 登记车辆问题的请求体。需要 Idempotency-Key 头；司机、物业与车辆由服务端取得。
/// </summary>
/// <param name="Description">问题描述，必填，最长 2000 字符。</param>
/// <param name="InspectionId">可选：关联本人的检查记录。关联不会自动把检查 FAIL 变成问题报告。</param>
public sealed record CreateVehicleIssueRequest(string? Description, string? InspectionId);

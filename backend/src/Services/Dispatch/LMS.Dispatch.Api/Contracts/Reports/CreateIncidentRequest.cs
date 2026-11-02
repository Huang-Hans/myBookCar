namespace LMS.Dispatch.Api.Contracts.Reports;

/// <summary>
/// 登记事故 / 事件报告的请求体。需要 Idempotency-Key 头；司机、物业与车辆由服务端取得。原型没有地点与附件，不设必填 GPS。
/// </summary>
/// <param name="Type">VEHICLE_BREAKDOWN / TRAFFIC_ACCIDENT / PASSENGER_ASSISTANCE / OTHER。</param>
/// <param name="OccurredAt">发生时间（ISO 8601 UTC，Z 结尾），不得晚于服务端当前时间；补报期限待业务确认，不设下限。</param>
/// <param name="Description">事件描述，必填，最长 2000 字符。</param>
/// <param name="TripId">可选：关联的行程标识，必须是本人的行程（含已完成 / 已取消）。</param>
public sealed record CreateIncidentRequest(string? Type, DateTimeOffset? OccurredAt, string? Description, string? TripId);

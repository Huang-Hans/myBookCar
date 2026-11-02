namespace LMS.Reporting.Api.Contracts.Schedules;

/// <summary>
/// 登记邮件报告计划。只登记计划，不表示邮件已发送或执行时间已确定；登记范围由服务端按调用者授权写入。
/// </summary>
/// <param name="RecipientEmail">收件邮箱，必填，必须是有效的邮件地址。</param>
/// <param name="Frequency">频率，必填：DAILY_0800 / WEEKLY_MONDAY / MONTHLY_FIRST_DAY。</param>
/// <param name="ReportType">报表类型，必填：DAILY_JOB_SUMMARY / TRIP_DETAIL / DRIVER_UTILIZATION / VEHICLE_UTILIZATION / CANCELLATION。</param>
public sealed record CreateReportScheduleRequest(string? RecipientEmail, string? Frequency, string? ReportType);

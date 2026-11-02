namespace LMS.Reporting.Api.Contracts.Schedules;

/// <summary>
/// 邮件报告计划视图，映射 report_schedules。
/// </summary>
/// <param name="ScheduleId">计划标识。</param>
/// <param name="RecipientEmail">收件邮箱（已规范为小写）。</param>
/// <param name="Frequency">频率：DAILY_0800 / WEEKLY_MONDAY / MONTHLY_FIRST_DAY。</param>
/// <param name="ReportType">报表类型。</param>
/// <param name="Version">记录版本。</param>
/// <param name="RegistrationStatus">恒为 REGISTERED，只代表已登记，不代表邮件已发送。</param>
public sealed record ReportScheduleView(
    string ScheduleId,
    string RecipientEmail,
    string Frequency,
    string ReportType,
    long Version,
    string RegistrationStatus);

using LMS.BuildingBlocks.Core.Scoping;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Domain.Schedules;

// 邮件报告计划的登记。登记只表达“已登记”，不表示邮件已发送或执行时间已确定。
// Scope 是登记时的授权范围快照，由服务端写入，不来自客户端；同一邮箱（忽略大小写）+ 频率 + 报表类型在可见范围内不重复。
public sealed record ReportSchedule(
    string ScheduleId,
    string RecipientEmail,
    string Frequency,
    ReportType ReportType,
    string CreatedBy,
    PropertyScope Scope,
    DateTimeOffset CreatedAt,
    long Version)
{
    private const string IdPrefix = "schedule_";
    private const long FirstVersion = 1;

    public static ReportSchedule Create(
        string recipientEmail,
        string frequency,
        ReportType reportType,
        string createdBy,
        PropertyScope scope,
        DateTimeOffset createdAt) =>
        new(IdPrefix + Guid.CreateVersion7().ToString("N"), NormalizeEmail(recipientEmail), frequency, reportType, createdBy, scope, createdAt, FirstVersion);

    public static string NormalizeEmail(string recipientEmail) => recipientEmail.Trim().ToLowerInvariant();

    // 调用者当前范围必须覆盖计划登记时的范围，才能看到或移除它。
    public bool IsVisibleTo(PropertyScope caller) =>
        Scope.IsAllProperties ? caller.IsAllProperties : caller.Allows(Scope.PropertyId!);
}

namespace LMS.Reporting.Infrastructure.Persistence;

// lms_reporting 库中的集合名。数据库内部名称不等于对外合同名称。
internal static class ReportingCollections
{
    public const string ReportRows = "report_rows";
    public const string ReportRefreshLocks = "report_refresh_locks";
    public const string ReportSchedules = "report_schedules";
    public const string ReportExports = "report_exports";
    public const string FileMetadata = "file_metadata";
    public const string AuditRecords = "audit_records";
    public const string IdempotencyRecords = "idempotency_records";
}

namespace LMS.Platform.Infrastructure.Persistence;

// lms_platform 库中的集合名。数据库内部名称不等于对外合同名称。
internal static class PlatformCollections
{
    public const string ConfigurationReleases = "configuration_releases";
    public const string ActiveConfigurations = "active_configurations";
    public const string AuditRecords = "audit_records";
    public const string IdempotencyRecords = "idempotency_records";
    public const string PortalUsers = "portal_users";
    public const string RolePermissions = "role_permissions";
    public const string CommonLocations = "common_locations";
    public const string CustomerBlacklists = "customer_blacklists";
    public const string ConfigurationAssets = "configuration_assets";
    public const string FileMetadata = "file_metadata";
    public const string ReportExports = "report_exports";
    public const string AppRefreshTokens = "app_refresh_tokens";
}

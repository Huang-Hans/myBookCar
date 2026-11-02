namespace LMS.Platform.Application.AuditRecords.Models;

public static class AuditRecordSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["occurredAt", "actor", "operation", "resourceType", "propertyId", "device", "sourceIp", "sessionReference", "outcome", "auditId"];

    public const string DefaultSort = "-occurredAt,auditId";
    public const string StableField = "auditId";
}

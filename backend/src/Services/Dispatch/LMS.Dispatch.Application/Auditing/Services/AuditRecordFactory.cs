using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Auditing;

namespace LMS.Dispatch.Application.Auditing.Services;

// 构造 Dispatch 本地的成功审计记录。变更摘要只记录字段名，不复制字段值（避免个人信息进入审计）。
internal static class AuditRecordFactory
{
    private const string ServiceName = "dispatch";

    public static AuditRecord Created(
        CommandContext context,
        string operation,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAt,
        string? propertyId) =>
        Succeeded(context, operation, resourceType, resourceId, occurredAt, "created", propertyId);

    public static AuditRecord Updated(
        CommandContext context,
        string operation,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAt,
        IReadOnlyList<string> changedFields,
        string? propertyId) =>
        Succeeded(context, operation, resourceType, resourceId, occurredAt, $"updated fields: {string.Join(',', changedFields)}", propertyId);

    public static AuditRecord Succeeded(
        CommandContext context,
        string operation,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAt,
        string changeSummary,
        string? propertyId) =>
        new(
            AuditRecord.NewId(),
            context.Actor,
            operation,
            resourceType,
            resourceId,
            occurredAt,
            AuditOutcome.Succeeded,
            context.TraceId,
            ServiceName,
            changeSummary,
            null,
            propertyId);
}

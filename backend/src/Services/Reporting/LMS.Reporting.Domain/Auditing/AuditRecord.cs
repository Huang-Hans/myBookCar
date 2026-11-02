namespace LMS.Reporting.Domain.Auditing;

// 技术审计事实：谁在何时对哪个资源做了什么，结果如何。字段见详细设计 17.3。
// ChangeSummary 只放经过筛选的变更摘要，不复制完整业务文档。
// PropertyId 是原型审计列“Property”的扩展，全物业管理操作为 null。
// Device、SourceIp、SessionReference 的可信来源尚未确定（Q11），当前写入路径不填充，保持 null。
public sealed record AuditRecord(
    string AuditId,
    string Actor,
    string Operation,
    string ResourceType,
    string ResourceId,
    DateTimeOffset OccurredAt,
    AuditOutcome Outcome,
    string TraceId,
    string SourceService,
    string? ChangeSummary,
    string? ReasonReference,
    string? PropertyId = null,
    string? Device = null,
    string? SourceIp = null,
    string? SessionReference = null)
{
    private const string IdPrefix = "aud_";

    public static string NewId() => IdPrefix + Guid.CreateVersion7().ToString("N");
}

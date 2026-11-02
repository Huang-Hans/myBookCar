namespace LMS.Platform.Api.Contracts.Audit;

/// <summary>
/// 审计记录视图，映射 audit_records：一次管理操作的可追溯记录。
/// </summary>
/// <param name="AuditId">审计记录标识。</param>
/// <param name="Actor">执行操作的操作人标识。</param>
/// <param name="Operation">执行的操作动作（Action）。</param>
/// <param name="ResourceType">操作目标资源的类型。</param>
/// <param name="ResourceId">操作目标资源的标识。</param>
/// <param name="OccurredAt">操作发生的 UTC 时间点。</param>
/// <param name="Outcome">操作结果，取值为 Succeeded / Failed 的既有字面值。</param>
/// <param name="TraceId">跨服务追踪用的关联链路标识。</param>
/// <param name="SourceService">产生该记录的来源服务。</param>
/// <param name="ChangeSummary">变更摘要；为空时省略该键。</param>
/// <param name="ReasonReference">操作原因的外部引用；为空时省略该键。</param>
/// <param name="PropertyId">操作涉及的物业代码；全物业管理操作时为空并省略。</param>
/// <param name="Device">可信且最小化的设备标签；为空时省略该键。</param>
/// <param name="SourceIp">来源 IP，IPv4 或 IPv6；为空时省略该键。</param>
/// <param name="SessionReference">会话的非敏感关联值，不存 Bearer/Cookie 原文；为空时省略。</param>
public sealed record AuditView(
    string AuditId,
    string Actor,
    string Operation,
    string ResourceType,
    string ResourceId,
    DateTime OccurredAt,
    string Outcome,
    string TraceId,
    string SourceService,
    string? ChangeSummary,
    string? ReasonReference,
    string? PropertyId,
    string? Device,
    string? SourceIp,
    string? SessionReference);

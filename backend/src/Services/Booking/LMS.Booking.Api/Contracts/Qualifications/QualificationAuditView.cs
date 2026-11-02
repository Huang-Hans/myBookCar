namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 资格操作审计记录：谁在何时对哪个资源做了什么、结果如何，以及资格操作的上下文。
/// </summary>
/// <param name="AuditId">审计记录标识。</param>
/// <param name="Actor">操作人标识。</param>
/// <param name="Operation">操作名称。</param>
/// <param name="ResourceType">操作目标类型。</param>
/// <param name="ResourceId">操作目标标识。</param>
/// <param name="OccurredAt">操作发生时间，UTC 时间点。</param>
/// <param name="Outcome">执行结果，保持现有字面值 Succeeded 或 Failed。</param>
/// <param name="TraceId">关联的 W3C trace id，与响应头 X-Trace-Id 一致。</param>
/// <param name="SourceService">产生该记录的来源服务。</param>
/// <param name="ChangeSummary">经过筛选的变更摘要；为空时省略该键，且不用它反推结构数据。</param>
/// <param name="ReasonReference">原因引用；为空时省略该键。</param>
/// <param name="PropertyId">相关物业代码；全物业管理操作为空并省略该键。</param>
/// <param name="Device">可信且最小化的设备标签；信任来源未定（Q11），当前写入路径不填充且为空并省略该键。</param>
/// <param name="SourceIp">来源 IP（IPv4/IPv6），必须来自可信来源；信任来源未定（Q11），当前写入路径不填充且为空并省略该键。</param>
/// <param name="SessionReference">与来源 IP / Session 关联的非敏感值，不得存放 Bearer Token 或 Cookie 原文；当前写入路径不填充且为空并省略该键。</param>
/// <param name="Qualification">资格审计的客人、说明与状态；非资格审计为空并省略该键。</param>
public sealed record QualificationAuditView(
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
    string? SessionReference,
    QualificationAuditContextView? Qualification);

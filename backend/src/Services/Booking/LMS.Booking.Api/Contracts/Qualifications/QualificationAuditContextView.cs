namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 资格审计的结构化上下文；只有资格相关审计记录才带该对象。
/// </summary>
/// <param name="GuestId">资格审计对应的客人标识。</param>
/// <param name="Justification">升级或批准例外时提交的必填说明。</param>
/// <param name="Status">资格升级审计记录状态，取大写下划线字面值 ESCALATED 或 APPROVED。</param>
public sealed record QualificationAuditContextView(string GuestId, string Justification, string Status);

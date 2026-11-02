namespace LMS.Dispatch.Api.Contracts.Reports;

/// <summary>
/// 车辆问题报告。status 只说明本系统的处理事实：RECORDED 表示已登记；目前没有运营 / 维修系统接入，
/// 不会出现 FORWARDED / ACKNOWLEDGED，更不表示已受理或已维修。
/// </summary>
/// <param name="IssueId">问题标识。</param>
/// <param name="PropertyId">登记时的所属物业。</param>
/// <param name="VehicleSnapshot">登记时的分配车辆文本。</param>
/// <param name="Description">问题描述。</param>
/// <param name="InspectionId">关联的检查记录；没有时省略。</param>
/// <param name="Status">RECORDED / FORWARDED / ACKNOWLEDGED。</param>
/// <param name="ReportedAt">服务端登记时间（UTC）。</param>
/// <param name="Version">记录版本号。</param>
public sealed record VehicleIssueView(
    string IssueId,
    string PropertyId,
    string VehicleSnapshot,
    string Description,
    string? InspectionId,
    string Status,
    DateTimeOffset ReportedAt,
    long Version);

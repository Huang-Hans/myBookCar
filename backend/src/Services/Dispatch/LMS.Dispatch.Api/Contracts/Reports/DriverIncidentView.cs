namespace LMS.Dispatch.Api.Contracts.Reports;

/// <summary>
/// 事故报告。保存、转交、签收是不同的事实：RECORDED 只表示本系统已保存；目前没有运营接入，不会出现 FORWARDED / ACKNOWLEDGED，
/// 也不表示拨号已接通或运营已受理。
/// </summary>
/// <param name="IncidentId">报告标识。</param>
/// <param name="PropertyId">登记时的所属物业。</param>
/// <param name="VehicleSnapshot">登记时的分配车辆文本。</param>
/// <param name="Type">事件类型。</param>
/// <param name="OccurredAt">司机陈述的发生时间（UTC）。</param>
/// <param name="Description">事件描述。</param>
/// <param name="TripId">关联行程；没有时省略。</param>
/// <param name="Status">RECORDED / FORWARDED / ACKNOWLEDGED。</param>
/// <param name="ReportedAt">服务端登记时间（UTC）。</param>
/// <param name="Version">记录版本号。</param>
public sealed record DriverIncidentView(
    string IncidentId,
    string PropertyId,
    string VehicleSnapshot,
    string Type,
    DateTimeOffset OccurredAt,
    string Description,
    string? TripId,
    string Status,
    DateTimeOffset ReportedAt,
    long Version);

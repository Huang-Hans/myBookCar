namespace LMS.Dispatch.Application.Reports.Models;

// 车辆问题与事故报告列表共用的排序约定：按服务端登记时间倒序，稳定分页。
public static class ReportSorting
{
    public static IReadOnlyList<string> VehicleIssueFields { get; } = ["reportedAt", "issueId"];

    public static IReadOnlyList<string> IncidentFields { get; } = ["reportedAt", "occurredAt", "incidentId"];

    public const string VehicleIssueDefaultSort = "-reportedAt,issueId";
    public const string VehicleIssueStableField = "issueId";
    public const string IncidentDefaultSort = "-reportedAt,incidentId";
    public const string IncidentStableField = "incidentId";
}

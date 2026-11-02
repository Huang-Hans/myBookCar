namespace LMS.Dispatch.Domain.Reports;

// 司机登记的车辆问题。独立于检查：可选关联同一司机的检查记录，但检查 FAIL 不会自动产生问题报告。
// 物业与车辆是登记时的主档快照；描述与时间由本人输入 / 服务端生成。
public sealed record VehicleIssue(
    string IssueId,
    string DriverId,
    string PropertyId,
    string VehicleSnapshot,
    string Description,
    string? InspectionId,
    VehicleIssueStatus Status,
    DateTimeOffset ReportedAt,
    long Version)
{
    public const string IdPrefix = "issue_";
    public const int MaxDescriptionLength = 2000;

    private const long FirstVersion = 1;

    public static VehicleIssue Create(
        string driverId,
        string propertyId,
        string vehicleSnapshot,
        string description,
        string? inspectionId,
        DateTimeOffset now) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            driverId, propertyId, vehicleSnapshot, description, inspectionId, VehicleIssueStatus.Recorded, now, FirstVersion);
}

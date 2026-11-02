namespace LMS.Dispatch.Domain.Inspections;

// 司机提交的一次检查记录。不可变：提交后不能被覆盖或修改，更正需另行提交新记录（修正规则待业务确认）。
// 物业与车辆是提交时从司机主档取的快照（车辆目前只有文本描述，没有车辆主档标识）。提交时间由服务端生成。
public sealed record DriverInspection(
    string InspectionId,
    string DriverId,
    string PropertyId,
    string VehicleSnapshot,
    InspectionPhase Phase,
    int TemplateVersion,
    IReadOnlyList<InspectionAnswer> Answers,
    DateTimeOffset SubmittedAt,
    long Version)
{
    public const string IdPrefix = "insp_";

    private const long FirstVersion = 1;

    public bool HasFailure => Answers.Any(answer => answer.Result == InspectionResult.Fail);

    public static DriverInspection Create(
        string driverId,
        string propertyId,
        string vehicleSnapshot,
        InspectionTemplate template,
        IReadOnlyList<InspectionAnswer> answers,
        DateTimeOffset now) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            driverId, propertyId, vehicleSnapshot, template.Phase, template.Version, answers, now, FirstVersion);
}

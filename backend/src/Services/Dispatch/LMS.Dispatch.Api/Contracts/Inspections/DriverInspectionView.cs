namespace LMS.Dispatch.Api.Contracts.Inspections;

/// <summary>单项检查结果。</summary>
public sealed record InspectionAnswerView(string ItemCode, string Result);

/// <summary>
/// 司机检查记录。提交后不可修改；物业与车辆是提交时的主档快照（车辆目前只有文本描述）。
/// </summary>
/// <param name="InspectionId">检查记录标识。</param>
/// <param name="Phase">PRE_TRIP / POST_TRIP。</param>
/// <param name="TemplateVersion">冻结的模板版本。</param>
/// <param name="PropertyId">提交时的所属物业。</param>
/// <param name="VehicleSnapshot">提交时的分配车辆文本。</param>
/// <param name="Answers">逐项结果。</param>
/// <param name="HasFailure">是否存在不合格项。</param>
/// <param name="SubmittedAt">服务端提交时间（UTC）。</param>
/// <param name="Version">记录版本号。</param>
public sealed record DriverInspectionView(
    string InspectionId,
    string Phase,
    int TemplateVersion,
    string PropertyId,
    string VehicleSnapshot,
    IReadOnlyList<InspectionAnswerView> Answers,
    bool HasFailure,
    DateTimeOffset SubmittedAt,
    long Version);

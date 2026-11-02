namespace LMS.Reporting.Api.Contracts.Reports;

/// <summary>
/// 车辆利用率的一行，映射 report_rows（VEHICLE_UTILIZATION）。
/// </summary>
/// <param name="RowId">报表行标识，只是查询行的技术标识，不是业务编号。</param>
/// <param name="VehicleDescription">车辆文本；正式车辆唯一标识待确认（Q02）。</param>
/// <param name="MileageMeters">里程，非负，单位米；未知时省略。</param>
/// <param name="TripCount">行程数，非负。</param>
/// <param name="DowntimeSeconds">停用时长（秒），非负；定义待确认（Q04），未知时省略。</param>
/// <param name="RedemptionReference">外部权益引用；为空时省略。</param>
/// <param name="Partial">为 true 时，所选区间内部分行程没有行驶轨迹，里程只含已知部分。</param>
/// <param name="UpdatedAt">来源读取模型的更新时间（UTC），不是点击生成的时刻。</param>
public sealed record VehicleUtilizationRowView(
    string RowId,
    string VehicleDescription,
    double? MileageMeters,
    int TripCount,
    long? DowntimeSeconds,
    string? RedemptionReference,
    bool Partial,
    DateTime UpdatedAt);

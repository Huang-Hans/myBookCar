namespace LMS.Reporting.Api.Contracts.Reports;

/// <summary>
/// 行程明细的一行，映射 report_rows（TRIP_DETAIL）。
/// </summary>
/// <param name="RowId">报表行标识，只是查询行的技术标识，不是业务编号。</param>
/// <param name="TripReference">行程引用。</param>
/// <param name="PropertyId">物业代码。</param>
/// <param name="GuestName">客人姓名。</param>
/// <param name="ScheduledAt">计划时间（UTC）。</param>
/// <param name="ActualAt">实际时间（UTC）；尚未发生（Awaiting pickup）时省略，不用演示时间填充。</param>
/// <param name="DriverId">司机稳定标识；为空时省略。</param>
/// <param name="DriverName">历史显示的司机名称；为空时省略。</param>
/// <param name="VehicleDescription">车辆文本；为空时省略。</param>
/// <param name="DistanceMeters">里程，非负，单位米；为空时省略。</param>
/// <param name="DurationSeconds">时长，非负，单位秒；为空时省略。</param>
/// <param name="Loyalty">外部权益引用。</param>
/// <param name="Source">权益系统的来源标签。</param>
/// <param name="UpdatedAt">来源读取模型的更新时间（UTC），不是点击生成的时刻。</param>
public sealed record TripDetailRowView(
    string RowId,
    string TripReference,
    string PropertyId,
    string GuestName,
    DateTime ScheduledAt,
    DateTime? ActualAt,
    string? DriverId,
    string? DriverName,
    string? VehicleDescription,
    double? DistanceMeters,
    long? DurationSeconds,
    LoyaltyReferenceView Loyalty,
    string Source,
    DateTime UpdatedAt);

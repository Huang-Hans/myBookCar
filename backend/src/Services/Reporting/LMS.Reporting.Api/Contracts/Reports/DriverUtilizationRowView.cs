namespace LMS.Reporting.Api.Contracts.Reports;

/// <summary>
/// 司机利用率的一行，映射 report_rows（DRIVER_UTILIZATION）。
/// </summary>
/// <param name="RowId">报表行标识，只是查询行的技术标识，不是业务编号。</param>
/// <param name="DriverId">稳定的司机引用。</param>
/// <param name="DriverName">司机姓名。</param>
/// <param name="PropertyId">物业代码。</param>
/// <param name="TripCount">行程数，非负。</param>
/// <param name="IdleSeconds">空闲时长（秒），非负；未知时省略。</param>
/// <param name="OvertimeSeconds">加班时长（秒），非负；定义待确认（Q04），未知时省略。</param>
/// <param name="RedemptionContributionReference">外部兑换贡献引用；为空时省略。</param>
/// <param name="Partial">为 true 时，所选区间内部分天的空闲或加班未知（当天没有当值班次），合计只含已知部分。</param>
/// <param name="UpdatedAt">来源读取模型的更新时间（UTC），不是点击生成的时刻。</param>
public sealed record DriverUtilizationRowView(
    string RowId,
    string DriverId,
    string DriverName,
    string PropertyId,
    int TripCount,
    long? IdleSeconds,
    long? OvertimeSeconds,
    string? RedemptionContributionReference,
    bool Partial,
    DateTime UpdatedAt);

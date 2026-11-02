namespace LMS.Reporting.Api.Contracts.Reports;

/// <summary>
/// 日报汇总的一行，映射 report_rows（DAILY_JOB_SUMMARY）。
/// </summary>
/// <param name="RowId">报表行标识，只是查询行的技术标识，不是业务编号。</param>
/// <param name="PropertyId">物业代码。</param>
/// <param name="Bookings">预约数，非负。</param>
/// <param name="Completed">完成数，非负。</param>
/// <param name="Cancellations">取消数，非负。</param>
/// <param name="NoShows">未到场数，非负。</param>
/// <param name="RedemptionSummary">外部权益引用摘要，不是金额合计；为空时省略。</param>
/// <param name="UpdatedAt">来源读取模型的更新时间（UTC），不是点击生成的时刻。</param>
public sealed record DailyJobSummaryRowView(
    string RowId,
    string PropertyId,
    int Bookings,
    int Completed,
    int Cancellations,
    int NoShows,
    string? RedemptionSummary,
    DateTime UpdatedAt);

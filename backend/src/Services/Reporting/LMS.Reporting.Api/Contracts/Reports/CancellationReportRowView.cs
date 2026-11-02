namespace LMS.Reporting.Api.Contracts.Reports;

/// <summary>
/// 取消情况的一行，映射 report_rows（CANCELLATION）。
/// </summary>
/// <param name="RowId">报表行标识，只是查询行的技术标识，不是业务编号。</param>
/// <param name="ReasonCode">取消原因代码，来自上传配置。</param>
/// <param name="Reason">取消原因显示名称。</param>
/// <param name="Source">取消来源：CUSTOMER / CUSTOMER_SERVICE / DRIVER。</param>
/// <param name="NoticeSecondsBeforePickup">取消时距上车的提前量（秒）；负值是否表示迟取消待确认（Q04），未知时省略。</param>
/// <param name="PointsReturnPolicyCode">积分退还政策代码；为空时省略。</param>
/// <param name="PointsReturnReference">积分退还的外部说明；为空时省略。</param>
/// <param name="UpdatedAt">来源读取模型的更新时间（UTC），不是点击生成的时刻。</param>
public sealed record CancellationReportRowView(
    string RowId,
    string ReasonCode,
    string Reason,
    string Source,
    long? NoticeSecondsBeforePickup,
    string? PointsReturnPolicyCode,
    string? PointsReturnReference,
    DateTime UpdatedAt);

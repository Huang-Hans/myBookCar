namespace LMS.Reporting.Application.Reports.Options;

// 报表读取模型的数据来源设置。五类报表的列级来源、统计区间与刷新规则（Q04）尚未确定，默认视为未接入：
// 此时报表查询与导出明确返回“来源不可用”（503 / 50300），不返回空结果冒充已生成。
// 只有 report_rows 的摄入真正接好之后，才把 RowsSourceConnected 设为 true，此后没有匹配行就是真实的空结果。
public sealed class ReportingOptions
{
    public const string SectionName = "Reporting";

    public bool RowsSourceConnected { get; init; }
}

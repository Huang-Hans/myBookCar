namespace LMS.Reporting.Domain.Rows;

// 报表行载荷的判别联合：每个 rowType 恰好对应一种载荷，不混装。
public abstract record ReportPayload;

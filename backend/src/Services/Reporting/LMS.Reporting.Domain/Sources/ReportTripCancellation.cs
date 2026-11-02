namespace LMS.Reporting.Domain.Sources;

// 行程的取消记录。CancelledBy 等于宾客标识时表示客户本人取消，否则为客服。
public sealed record ReportTripCancellation(string ReasonCode, string CancelledBy, DateTimeOffset CancelledAt, string? PolicyReference);

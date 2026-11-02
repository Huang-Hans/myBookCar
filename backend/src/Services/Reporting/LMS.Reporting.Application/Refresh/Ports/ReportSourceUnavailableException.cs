namespace LMS.Reporting.Application.Refresh.Ports;

// 报表来源暂时不可用：本轮跳过受影响的业务日，下一轮重试；已有的报表行保持不变。
public sealed class ReportSourceUnavailableException(string source, Exception? inner = null)
    : Exception($"The report source '{source}' is unavailable.", inner)
{
    public string SourceName { get; } = source;
}

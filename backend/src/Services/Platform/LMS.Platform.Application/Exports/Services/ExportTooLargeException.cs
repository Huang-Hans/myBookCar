namespace LMS.Platform.Application.Exports.Services;

// 导出结果超过允许的行数或文件大小：作业按失败结束，而不是生成残缺文件。
internal sealed class ExportTooLargeException : Exception
{
    public ExportTooLargeException()
        : base("The export exceeds the maximum number of rows or the maximum file size.")
    {
    }
}

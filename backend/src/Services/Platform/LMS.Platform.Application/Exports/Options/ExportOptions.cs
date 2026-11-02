namespace LMS.Platform.Application.Exports.Options;

// 导出作业设置（Q07 未给出保留窗口与执行约束，全部走部署配置，下面是保守默认值）。
public sealed class ExportOptions
{
    public const string SectionName = "Exports";

    // 结果文件保留时间；超过后不能再下载。
    public int ResultRetentionHours { get; init; } = 24;

    // 下载地址有效期；已签发的地址在失效前仍可使用，权限复核发生在签发时。
    public int DownloadUrlTtlMinutes { get; init; } = 5;

    // 作业租约：执行者超过该时间没有完成，作业可被重新领取，避免崩溃后永远停在 RUNNING。
    public int LeaseMinutes { get; init; } = 15;

    // 单个导出最多包含的行数，超过则作业失败而不是生成残缺文件。
    public int MaxRows { get; init; } = 100000;

    public int BatchSize { get; init; } = 500;

    // 结果文件大小上限（字节），超过则作业失败。
    public long MaxFileBytes { get; init; } = 256L * 1024 * 1024;

    // 空闲时的轮询间隔。
    public int PollSeconds { get; init; } = 5;
}

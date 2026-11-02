namespace LMS.Reporting.Application.Files.Options;

// 导出结果文件的存储设置（Q07 未给出协议，全部走部署配置）。任何一项缺失，导出申请与下载都明确返回“来源不可用”。
// SigningKey 是机密（至少 32 字符），通过用户机密或环境变量提供；PublicBaseUrl 是客户端能访问到下载端点的外部地址（通常是网关）。
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public const int MinimumSigningKeyLength = 32;

    public string RootPath { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(RootPath)
        && Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _)
        && SigningKey.Length >= MinimumSigningKeyLength;
}

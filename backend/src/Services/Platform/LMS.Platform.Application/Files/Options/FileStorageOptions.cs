namespace LMS.Platform.Application.Files.Options;

// 文件上传与存储设置（Q07 未给出协议，全部走部署配置）。任何一项缺失，文件登记与上传都明确返回“来源不可用”。
// SigningKey 是机密（至少 32 字符），通过用户机密或环境变量提供；PublicBaseUrl 是客户端能访问到上传端点的外部地址（通常是网关）。
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public const int MinimumSigningKeyLength = 32;

    public string RootPath { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int UploadUrlTtlMinutes { get; init; } = 5;

    public long MaxSizeBytes { get; init; }

    public string[] AllowedContentTypes { get; init; } = [];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(RootPath)
        && Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _)
        && SigningKey.Length >= MinimumSigningKeyLength
        && UploadUrlTtlMinutes > 0
        && MaxSizeBytes > 0
        && AllowedContentTypes.Length > 0;
}

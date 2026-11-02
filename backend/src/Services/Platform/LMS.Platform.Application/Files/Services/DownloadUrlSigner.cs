using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LMS.Platform.Application.Exports.Options;
using LMS.Platform.Application.Files.Options;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Files.Services;

// 生成并校验短效、单文件的下载 URL。签名覆盖用途、文件标识和失效时间，与上传签名互不通用；校验用常量时间比较。
internal sealed class DownloadUrlSigner(
    IOptions<FileStorageOptions> storageOptions,
    IOptions<ExportOptions> exportOptions,
    TimeProvider timeProvider)
{
    private const string DownloadPath = "/api/platform/staff/v1/file-downloads/";
    private const string Purpose = "download";

    public (string Url, DateTimeOffset ExpiresAt) Create(string fileId)
    {
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(exportOptions.Value.DownloadUrlTtlMinutes);
        var expires = expiresAt.ToUnixTimeSeconds();
        var baseUrl = storageOptions.Value.PublicBaseUrl.TrimEnd('/');

        return ($"{baseUrl}{DownloadPath}{Uri.EscapeDataString(fileId)}?expires={expires}&signature={Sign(fileId, expires)}", expiresAt);
    }

    public bool IsValid(string fileId, long expiresAtUnixSeconds, string signature)
    {
        if (timeProvider.GetUtcNow().ToUnixTimeSeconds() >= expiresAtUnixSeconds)
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(Sign(fileId, expiresAtUnixSeconds));
        return CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(signature));
    }

    private string Sign(string fileId, long expiresAtUnixSeconds)
    {
        var key = Encoding.UTF8.GetBytes(storageOptions.Value.SigningKey);
        var payload = Encoding.UTF8.GetBytes($"{Purpose}|{fileId}|{expiresAtUnixSeconds.ToString(CultureInfo.InvariantCulture)}");

        return Convert.ToHexString(HMACSHA256.HashData(key, payload)).ToLowerInvariant();
    }
}

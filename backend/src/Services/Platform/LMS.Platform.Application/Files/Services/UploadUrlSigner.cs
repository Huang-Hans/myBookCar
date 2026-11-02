using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LMS.Platform.Application.Files.Models;
using LMS.Platform.Application.Files.Options;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Files.Services;

// 生成并校验短效、单文件的上传 URL。签名覆盖文件标识和失效时间，不含用户信息；校验用常量时间比较。
internal sealed class UploadUrlSigner(IOptions<FileStorageOptions> options, TimeProvider timeProvider)
{
    private const string UploadPath = "/api/platform/staff/v1/file-uploads/";
    private const string UploadMethod = "PUT";

    public UploadDescription Create(string fileId, string contentType)
    {
        var settings = options.Value;
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(settings.UploadUrlTtlMinutes);
        var expires = expiresAt.ToUnixTimeSeconds();
        var url = $"{settings.PublicBaseUrl.TrimEnd('/')}{UploadPath}{Uri.EscapeDataString(fileId)}?expires={expires}&signature={Sign(fileId, expires)}";

        return new UploadDescription(url, UploadMethod, contentType, expiresAt);
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
        var key = Encoding.UTF8.GetBytes(options.Value.SigningKey);
        var payload = Encoding.UTF8.GetBytes($"{fileId}|{expiresAtUnixSeconds.ToString(CultureInfo.InvariantCulture)}");

        return Convert.ToHexString(HMACSHA256.HashData(key, payload)).ToLowerInvariant();
    }
}

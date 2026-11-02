using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace LMS.Platform.Application.Authentication.Services;

// 刷新凭据：256 位随机数的 URL 安全编码明文，以及其 SHA-256 摘要。服务端只保存摘要（高熵随机值不需要加盐慢哈希）。
internal readonly record struct RefreshTokenSecret(string Plain, string Hash)
{
    private const int EntropyBytes = 32;

    public static RefreshTokenSecret Create()
    {
        var plain = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(EntropyBytes));

        return new RefreshTokenSecret(plain, HashOf(plain));
    }

    public static string HashOf(string plain) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plain)));
}

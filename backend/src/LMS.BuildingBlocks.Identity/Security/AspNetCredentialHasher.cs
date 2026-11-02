using LMS.BuildingBlocks.Core.Security;
using Microsoft.AspNetCore.Identity;

namespace LMS.BuildingBlocks.Identity.Security;

// 基于 ASP.NET Core Identity 的 PasswordHasher（PBKDF2，随机盐，哈希中带版本与迭代参数）。
// 哈希参数需要升级时仍视为校验成功；本期没有改密/重哈希流程，因此不在登录路径里改写存储。
internal sealed class AspNetCredentialHasher : ICredentialHasher
{
    private static readonly object Owner = new();
    private readonly PasswordHasher<object> hasher = new();

    public string Hash(string password) => hasher.HashPassword(Owner, password);

    public bool Verify(string? storedHash, string password)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        try
        {
            return hasher.VerifyHashedPassword(Owner, storedHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

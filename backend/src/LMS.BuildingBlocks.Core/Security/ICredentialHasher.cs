namespace LMS.BuildingBlocks.Core.Security;

// 司机与客户共用的密码哈希与校验。哈希结果带算法参数与盐，不可逆；明文只在校验当次使用，不存库、不记录。
public interface ICredentialHasher
{
    string Hash(string password);

    // 存储的哈希为空、格式无法识别或与明文不匹配时返回 false。
    bool Verify(string? storedHash, string password);
}

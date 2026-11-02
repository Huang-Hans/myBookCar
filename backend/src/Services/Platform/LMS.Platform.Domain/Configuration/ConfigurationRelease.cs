namespace LMS.Platform.Domain.Configuration;

// 一次配置发布的元数据：只记录版本、摘要、批准人、生效环境与时间（详细设计 18.3），不保存配置正文。
// Sequence 在同一环境内从 1 递增，回滚通过发布新的版本完成，历史发布记录不修改。
public sealed record ConfigurationRelease(
    string ReleaseId,
    string Environment,
    long Sequence,
    string Digest,
    string ApprovedBy,
    DateTimeOffset EffectiveAt,
    DateTimeOffset CreatedAt)
{
    private const string IdPrefix = "cfgrel_";

    public static ConfigurationRelease Create(
        string environment,
        long sequence,
        string digest,
        string approvedBy,
        DateTimeOffset effectiveAt,
        DateTimeOffset createdAt) =>
        new(IdPrefix + Guid.CreateVersion7().ToString("N"), environment, sequence, digest, approvedBy, effectiveAt, createdAt);
}

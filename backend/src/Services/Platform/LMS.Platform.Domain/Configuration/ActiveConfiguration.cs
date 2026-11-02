namespace LMS.Platform.Domain.Configuration;

// 某个环境当前生效的发布。Version 是并发控制版本：每次切换递增，更新必须带上期望版本。
public sealed record ActiveConfiguration(
    string Environment,
    string ReleaseId,
    long Sequence,
    long Version,
    DateTimeOffset UpdatedAt)
{
    private const long FirstVersion = 1;

    public static ActiveConfiguration Start(ConfigurationRelease release) =>
        new(release.Environment, release.ReleaseId, release.Sequence, FirstVersion, release.CreatedAt);

    public ActiveConfiguration Advance(ConfigurationRelease release) =>
        this with { ReleaseId = release.ReleaseId, Sequence = release.Sequence, Version = Version + 1, UpdatedAt = release.CreatedAt };
}

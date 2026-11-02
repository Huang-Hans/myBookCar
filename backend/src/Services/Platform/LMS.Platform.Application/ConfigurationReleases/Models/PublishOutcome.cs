namespace LMS.Platform.Application.ConfigurationReleases.Models;

public enum PublishOutcome
{
    // 本次请求创建了新的发布。
    Created,

    // 相同幂等键且内容一致，返回此前已提交的发布。
    Replayed,

    // 期望的生效版本与当前不一致（含并发竞争失败）。后续 Api 层映射为 HTTP 409 / 40900。
    VersionConflict,

    // 相同幂等键但请求内容不同。后续 Api 层映射为 HTTP 409 / 40900。
    IdempotencyKeyReused
}

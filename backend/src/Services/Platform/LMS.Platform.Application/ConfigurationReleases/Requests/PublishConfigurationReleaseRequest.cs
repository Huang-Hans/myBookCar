using System.Globalization;
using System.Text.Json;

namespace LMS.Platform.Application.ConfigurationReleases.Requests;

// ExpectedActiveVersion 为 null 表示该环境此前没有生效发布。
// Actor、TraceId 由调用方（后续的 Api 层）从认证主体和当前请求取得；字段格式校验也在 Api 层完成。
public sealed record PublishConfigurationReleaseRequest(
    string Environment,
    string Digest,
    string ApprovedBy,
    DateTimeOffset EffectiveAt,
    long? ExpectedActiveVersion,
    string Actor,
    string TraceId,
    string IdempotencyKey)
{
    // 只包含语义输入，不含 Actor、TraceId 和幂等键本身。
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new[]
        {
            Environment,
            Digest,
            ApprovedBy,
            EffectiveAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            ExpectedActiveVersion?.ToString(CultureInfo.InvariantCulture) ?? string.Empty
        });
}

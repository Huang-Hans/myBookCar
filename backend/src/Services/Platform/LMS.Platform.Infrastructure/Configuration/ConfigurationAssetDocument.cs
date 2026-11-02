using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration;

// 配置正文：每个（发布，区域）一份强类型正文。_id 由发布标识和区域代码组成，
// 因此正文总是绑定到一次具体的发布（Q10），生效指针切换后读取到的就是新发布的正文。
internal sealed class ConfigurationAssetDocument<TPayload> where TPayload : class
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    [BsonElement("releaseId")]
    public string ReleaseId { get; init; } = string.Empty;

    [BsonElement("areaCode")]
    public string AreaCode { get; init; } = string.Empty;

    [BsonElement("payload")]
    public TPayload Payload { get; init; } = null!;

    public static string ComposeId(string releaseId, string areaCode) => $"{releaseId}:{areaCode}";
}

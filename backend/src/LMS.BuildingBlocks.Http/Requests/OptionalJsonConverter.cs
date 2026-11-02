using System.Text.Json;
using System.Text.Json.Serialization;
using LMS.BuildingBlocks.Core.Patching;

namespace LMS.BuildingBlocks.Http.Requests;

// 键出现在 JSON 中即标记 IsSet（包括显式 null）；键缺失时保持默认的未传状态。
// 是否允许 null 由字段校验决定，转换器本身不做限制。
internal sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override bool HandleNull => true;

    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Optional<T>.Of(default);
        }

        return Optional<T>.Of(JsonSerializer.Deserialize<T>(ref reader, options));
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Optional values are request-only and are never serialized.");
}

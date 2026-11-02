using System.Text.Json;
using System.Text.Json.Serialization;

namespace LMS.BuildingBlocks.Http.Requests;

// 请求正文的严格序列化选项：未知字段、重复字段、数字字符串都拒绝，不静默忽略。
internal static class RequestJson
{
    public static readonly JsonSerializerOptions Options = Create();

    public static readonly JsonDocumentOptions DocumentOptions = new() { AllowDuplicateProperties = false };

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            AllowDuplicateProperties = false,
            NumberHandling = JsonNumberHandling.Strict,
            PropertyNameCaseInsensitive = false
        };
        options.Converters.Add(new OptionalJsonConverterFactory());

        return options;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace LMS.BuildingBlocks.Http.Serialization;

// 统一响应体的 JSON 约定：camelCase 字段名、省略 null、枚举按 SCREAMING_SNAKE_CASE 字面值输出。
// 同一套约定同时注册到写出响应用的 Http.Json 与文档元数据用的 Mvc.Json，
// 确保 API 文档里看到的字段名和枚举取值与实际响应一致。
public static class ApiJsonConventions
{
    public static void Apply(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
    }
}

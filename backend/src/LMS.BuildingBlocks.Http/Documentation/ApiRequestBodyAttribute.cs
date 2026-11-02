namespace LMS.BuildingBlocks.Http.Documentation;

/// <summary>
/// 声明动作的请求正文，供 Swagger 显示请求体。
/// 动作的正文由 <c>JsonRequestBody.ReadAsync</c> 从 HttpContext 严格读取，没有 [FromBody] 形参，
/// ApiExplorer 与 Swashbuckle 无法从签名推断正文，因此需要在这里显式声明。
/// </summary>
/// <param name="requestType">正文契约类型；非 JSON 正文（例如上传的二进制流）只需给出占位类型，schema 按二进制描述。</param>
/// <param name="contentType">正文的媒体类型。</param>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class ApiRequestBodyAttribute(Type requestType, string contentType = "application/json") : Attribute
{
    /// <summary>JSON 正文的媒体类型；请求方必须以此类型提交，否则返回 415。</summary>
    public const string JsonContentType = "application/json";

    /// <summary>正文契约类型。</summary>
    public Type RequestType { get; } = requestType;

    /// <summary>正文的媒体类型。</summary>
    public string ContentType { get; } = contentType;
}

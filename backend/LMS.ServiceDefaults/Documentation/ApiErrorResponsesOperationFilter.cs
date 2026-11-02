using System.Globalization;
using LMS.BuildingBlocks.Http.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMS.ServiceDefaults.Documentation;

// 给每个业务操作补充统一失败响应，使文档里也能看到失败响应体（ApiErrorEnvelope）：
// 请求无法解析或字段校验失败（400 / 40000、40001）、未认证或令牌过期（401 / 40100、40101）、
// 无权限或超出物业范围（403 / 40300）、未处理异常（500 / 50000）。这四类对任何受保护的业务路由都成立。
// 各操作特有的 404 / 409 / 422 / 503 由 docs/api/openapi 下登记的契约文件描述，此处不重复声明。
internal sealed class ApiErrorResponsesOperationFilter : IOperationFilter
{
    private const string JsonMediaType = "application/json";

    private static readonly (int StatusCode, string Description)[] StandardErrors =
    [
        (StatusCodes.Status400BadRequest, "请求无法解析（40000）或字段校验失败（40001）。"),
        (StatusCodes.Status401Unauthorized, "未认证（40100）或令牌已过期（40101）。"),
        (StatusCodes.Status403Forbidden, "缺少操作权限，或超出调用者的物业范围（40300）。"),
        (StatusCodes.Status500InternalServerError, "服务端未处理异常（50000）；详情仅记录在服务端日志，用 traceId 关联。"),
    ];

    // 控制器用 ProducesResponseType 声明、没有自带说明的响应，生成器只会填 HTTP 原因短语（英文）；这里换成中文。
    private static readonly IReadOnlyDictionary<int, string> DeclaredResponseDescriptions = new Dictionary<int, string>
    {
        [StatusCodes.Status200OK] = "成功。",
        [StatusCodes.Status400BadRequest] = "请求无法解析（40000）或字段校验失败（40001）。",
        [StatusCodes.Status401Unauthorized] = "凭据或刷新令牌无效（40100），调用方应重新登录。",
        [StatusCodes.Status500InternalServerError] = "服务端未处理异常（50000）；用 traceId 关联服务端日志。",
        [StatusCodes.Status415UnsupportedMediaType] = "请求的 Content-Type 不受支持，须为 application/json（40000）。",
        [StatusCodes.Status502BadGateway] = "身份提供方或下游服务返回了无法处理的响应（50200）。",
        [StatusCodes.Status503ServiceUnavailable] = "服务暂不可用，或身份提供方未配置（50300）。",
        [StatusCodes.Status504GatewayTimeout] = "身份提供方或下游服务响应超时（50400）。"
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        TranslateDeclaredResponses(operation);
        var allowsAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

        foreach (var (statusCode, description) in StandardErrors)
        {
            if (allowsAnonymous && statusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
            {
                continue;
            }

            var statusKey = statusCode.ToString(CultureInfo.InvariantCulture);
            if (operation.Responses.ContainsKey(statusKey))
            {
                continue;
            }

            operation.Responses.Add(statusKey, CreateErrorResponse(context, description));
        }
    }

    private static void TranslateDeclaredResponses(OpenApiOperation operation)
    {
        foreach (var (key, response) in operation.Responses)
        {
            if (int.TryParse(key, CultureInfo.InvariantCulture, out var statusCode) &&
                response.Description == ReasonPhrases.GetReasonPhrase(statusCode) &&
                DeclaredResponseDescriptions.TryGetValue(statusCode, out var description))
            {
                response.Description = description;
            }
        }
    }

    private static OpenApiResponse CreateErrorResponse(OperationFilterContext context, string description) => new()
    {
        Description = description,
        Content =
        {
            [JsonMediaType] = new OpenApiMediaType
            {
                Schema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorEnvelope), context.SchemaRepository)
            }
        }
    };
}

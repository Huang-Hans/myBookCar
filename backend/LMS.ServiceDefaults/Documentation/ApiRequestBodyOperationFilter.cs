using System.Reflection;
using LMS.BuildingBlocks.Http.Documentation;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMS.ServiceDefaults.Documentation;

// 把 [ApiRequestBody] 声明的请求正文写进文档，使接口文档能看到请求体与其字段说明。
// 业务动作的正文由 JsonRequestBody 严格读取，没有 [FromBody] 形参，绑定层面无法提供正文信息。
internal sealed class ApiRequestBodyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<ApiRequestBodyAttribute>() is not { } declaration)
        {
            return;
        }

        // 已由真实绑定形参生成正文时保持原样，声明只补 ApiExplorer 看不到的正文。
        operation.RequestBody ??= new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                [declaration.ContentType] = new OpenApiMediaType { Schema = CreateSchema(declaration, context) }
            }
        };
    }

    // JSON 正文按契约类型生成 schema；其他媒体类型是二进制载荷，只有传输形式可描述。
    private static OpenApiSchema CreateSchema(ApiRequestBodyAttribute declaration, OperationFilterContext context) =>
        declaration.ContentType == ApiRequestBodyAttribute.JsonContentType
            ? context.SchemaGenerator.GenerateSchema(declaration.RequestType, context.SchemaRepository)
            : new OpenApiSchema { Type = "string", Format = "binary" };
}

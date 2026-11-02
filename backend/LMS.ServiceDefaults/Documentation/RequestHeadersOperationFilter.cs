using System.Reflection;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMS.ServiceDefaults.Documentation;

// 动作用 RequestHeaders 读取请求头、没有对应形参，这里按特性把它们声明为 header 参数。
internal sealed class RequestHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<IdempotencyKeyHeaderAttribute>() is not null)
        {
            operation.Parameters.Add(CreateParameter(
                RequestHeaders.IdempotencyKey,
                required: true,
                "幂等键，必填；相同键且内容一致的重试返回首次结果，同一键换了内容返回 409。缺失或空白返回 400（40000）。",
                new OpenApiSchema { Type = "string" }));
        }
    }

    private static OpenApiParameter CreateParameter(string name, bool required, string description, OpenApiSchema schema) => new()
    {
        Name = name,
        In = ParameterLocation.Header,
        Required = required,
        Description = description,
        Schema = schema
    };
}

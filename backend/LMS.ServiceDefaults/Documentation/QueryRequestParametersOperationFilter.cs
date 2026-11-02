using System.ComponentModel.DataAnnotations;
using System.Reflection;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.BuildingBlocks.Http.Requests.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LMS.ServiceDefaults.Documentation;

// 从实际查询请求类及其初始值生成参数文档，与接口绑定使用同一份定义。
internal sealed class QueryRequestParametersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameter = context.MethodInfo.GetParameters().FirstOrDefault(IsQueryRequest);
        if (parameter is null)
        {
            return;
        }

        var defaults = (QueryRequest)Activator.CreateInstance(parameter.ParameterType)!;
        RemoveGeneratedQueryParameters(operation);
        foreach (var property in QueryRequestMetadata.Read(defaults))
        {
            operation.Parameters.Add(CreateParameter(property, defaults));
        }
    }

    private static bool IsQueryRequest(ParameterInfo parameter) =>
        parameter.GetCustomAttribute<FromQueryAttribute>() is not null &&
        typeof(QueryRequest).IsAssignableFrom(parameter.ParameterType);

    private static void RemoveGeneratedQueryParameters(OpenApiOperation operation)
    {
        foreach (var parameter in operation.Parameters.Where(item => item.In == ParameterLocation.Query).ToList())
        {
            operation.Parameters.Remove(parameter);
        }
    }

    private static bool IsSort(QueryPropertyMetadata property) =>
        property.Property.DeclaringType == typeof(PagedQueryRequest) && property.Property.Name == nameof(PagedQueryRequest.Sort);

    private static OpenApiParameter CreateParameter(QueryPropertyMetadata property, QueryRequest defaults)
    {
        var spec = IsSort(property) && defaults is PagedQueryRequest paged ? paged.GetSortSpecification() : null;
        return new OpenApiParameter
        {
            Name = property.Name,
            In = ParameterLocation.Query,
            Required = property.Property.GetCustomAttribute<RequiredAttribute>() is not null,
            Description = spec is null ? property.Description : $"{property.Description} 可用字段：{string.Join(", ", spec.SortFields)}。",
            Schema = QueryParameterSchemaFactory.Create(property, spec?.DefaultSort ?? property.DefaultValue)
        };
    }
}

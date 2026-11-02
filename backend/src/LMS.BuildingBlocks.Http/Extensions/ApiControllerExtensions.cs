using LMS.BuildingBlocks.Http.Serialization;
using LMS.BuildingBlocks.Http.Requests.Binding;
using LMS.BuildingBlocks.Http.Requests.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.BuildingBlocks.Http.Extensions;

public static class ApiControllerExtensions
{
    // 注册 MVC 控制器。请求体、查询串与请求头由 JsonRequestBody / QueryReader / RequestHeaders 严格读取，
    // 因此关闭 ApiController 的自动模型校验与 ProblemDetails 映射，所有错误都走统一响应体。
    // Json 约定同时注册到 MVC：Swagger 由它推导 schema，字段名与枚举取值需与实际响应一致。
    public static IMvcBuilder AddApiControllers(this IServiceCollection services) =>
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
            options.SuppressMapClientErrors = true;
        }).AddMvcOptions(options =>
        {
            options.ModelBinderProviders.Insert(0, new QueryRequestModelBinderProvider());
            options.Filters.Add<QueryRequestValidationFilter>();
        }).AddJsonOptions(options => ApiJsonConventions.Apply(options.JsonSerializerOptions));
}

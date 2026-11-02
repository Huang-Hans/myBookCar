using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.BuildingBlocks.Http.Extensions;

public static class ApiHttpExtensions
{
    public static IServiceCollection AddApiHttpConventions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(ConfigureJson);
        services.AddExceptionHandler<ApiExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app)
    {
        // 响应由已注册的 IExceptionHandler 写出，因此这里传入的管道回调不会被触发。
        app.UseExceptionHandler(static _ => { });
        app.UseStatusCodePages(ApiStatusCodeResponses.WriteAsync);
        return app;
    }

    private static void ConfigureJson(JsonOptions options) => ApiJsonConventions.Apply(options.SerializerOptions);
}

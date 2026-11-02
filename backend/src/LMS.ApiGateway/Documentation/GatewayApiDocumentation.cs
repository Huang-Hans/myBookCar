using LMS.ApiGateway.Documentation.Sources;
using LMS.ApiGateway.Options;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace LMS.ApiGateway.Documentation;

// Swashbuckle serves the same documents to Swagger UI at /swagger and Scalar at /scalar.
// 下拉菜单默认打开“全部服务”，并保留网关自身与各下游服务的独立文档。
// The gateway document includes login; banners and proxy routes remain outside its scope.
internal static class GatewayApiDocumentation
{
    private const string UiPath = "/swagger";
    private const string ScalarUiPath = "/scalar";
    private const string GatewayDocumentPath = "/swagger/v1/swagger.json";

    // 聚合与下游文档的路径不能放在 /swagger 下，原因见 DownstreamDocuments。
    private const string AggregatedDocumentPath = "/openapi/all.json";

    private const string JsonContentType = "application/json";

    public static IServiceCollection AddGatewayApiDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GatewayDocumentationOptions>()
            .Bind(configuration.GetSection(GatewayDocumentationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<DownstreamDocumentStore>();
        services.AddSingleton<GatewayDocumentSource>();
        services.AddSingleton<AggregatedDocumentProvider>();

        return services;
    }

    public static WebApplication MapGatewayApiDocumentation(this WebApplication app)
    {
        // 文档界面与取文档的端点都属于文档骨架，不进 API 文档本身。
        app.UseSwagger();
        app.UseSwaggerUI(ConfigureSwaggerUi);
        app.MapScalarApiReference(ScalarUiPath, ConfigureScalar).ExcludeFromDescription();

        app.MapGet(AggregatedDocumentPath, ServeAggregatedDocument)
            .WithName("getAggregatedApiDocument")
            .ExcludeFromDescription();

        foreach (var source in DownstreamDocuments.All)
        {
            app.MapGet(source.Path, (DownstreamDocumentStore store, CancellationToken cancellationToken) =>
                    ServeDownstreamDocument(store, source, cancellationToken))
                .WithName($"get{char.ToUpperInvariant(source.Key[0])}{source.Key[1..]}ApiDocument")
                .ExcludeFromDescription();
        }

        return app;
    }

    private static void ConfigureSwaggerUi(SwaggerUIOptions options)
    {
        options.RoutePrefix = UiPath.TrimStart('/');
        options.DocumentTitle = "LMS API Gateway";
        options.HeadContent = $"""<link rel="icon" type="image/png" href="{Extensions.WinsonFaviconDataUrl}" />""";
        // 第一个文档是打开页面时的默认视图，因此“全部服务”必须排在最前。
        options.SwaggerEndpoint(AggregatedDocumentPath, "全部服务");
        options.SwaggerEndpoint(GatewayDocumentPath, "LMS.ApiGateway");
        foreach (var source in DownstreamDocuments.All)
        {
            options.SwaggerEndpoint(source.Path, source.Title);
        }
    }

    private static void ConfigureScalar(ScalarOptions options)
    {
        options.WithTitle("LMS API Gateway");
        options.AddPreferredSecuritySchemes(["bearerAuth", "lmsAppBearer"]);
        options.HideModels();
        options.WithFavicon(Extensions.WinsonFaviconDataUrl);
        options.AddDocument("all", "全部服务", AggregatedDocumentPath);
        options.AddDocument("gateway", "LMS.ApiGateway", GatewayDocumentPath);
        foreach (var source in DownstreamDocuments.All)
        {
            options.AddDocument(source.Key, source.Title, source.Path);
        }
    }

    // 所有下游都不可用时返回网关自己的 502，界面据此提示，而不是把异常抛给浏览器。
    private static async Task<IResult> ServeAggregatedDocument(AggregatedDocumentProvider provider, CancellationToken cancellationToken)
    {
        var json = await provider.GetAsync(cancellationToken);

        return json is null ? BadGateway() : Results.Text(json, JsonContentType);
    }

    private static async Task<IResult> ServeDownstreamDocument(
        DownstreamDocumentStore store,
        DownstreamDocument source,
        CancellationToken cancellationToken)
    {
        var document = await store.GetAsync(source, cancellationToken);

        return document is null ? BadGateway() : Results.Text(document.Json, JsonContentType);
    }

    private static IResult BadGateway() => ApiResults.Fail(ApiErrors.FromStatusCode(StatusCodes.Status502BadGateway));
}

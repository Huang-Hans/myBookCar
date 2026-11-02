using System.Reflection;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Extensions;
using LMS.ServiceDefaults.Configuration;
using LMS.ServiceDefaults.Documentation;
using LMS.ServiceDefaults.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.Hosting;

// 注册通用 Aspire 服务：服务发现、弹性、健康检查与 OpenTelemetry。
// 解决方案中的每个服务项目都应引用本项目。
// 更多说明见 https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string LivenessEndpointPath = "/healthz";
    private const string ReadinessEndpointPath = "/readyz";
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";
    private const string HealthEndpointsEnabledKey = "HealthEndpoints:Enabled";
    private const string ApiDocumentationPath = "/swagger/v1/swagger.json";
    private const string ApiDocumentationUiPath = "/swagger";
    private const string ScalarApiDocumentationUiPath = "/scalar";

    /// <summary>
    /// 永燊集团（Winson Group）标准 Favicon 图标（64x64 PNG，Data URL 格式）。
    /// </summary>
    private static readonly string FaviconHeadContent =
        $"""<link rel="icon" type="image/png" href="{WinsonFaviconDataUrl}" />""";

    public const string WinsonFaviconDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAMAAACdt4HsAAAAXVBMVEUAAABIRP9IRP9IRP9IRP9IRP9IRP9IRP9IRP9IRP9IRP9IRP9IRP9IRP9IRP9JRf9IRf9IRP5IRP5JRP9JRf////+Vk/+zsv++vf9mY/9taf9/fP+Jh//c3P9RTf/Ibar2AAAAFHRSTlMA/MJX+fPt48cahaff1dMaGqcahSpTPmAAAAEvSURBVFjD7dfNTsMwEATgXcf5cdKEtixtcYH3f0xEibIguba1g8Slc+58iqUedmhNaMbJzX3Xema5G2bfdv3iprE50s80g7Yqw0Oz1Z+cmOL2a38nxuy+BSfmuNv7BUhDRAMCDESBEYCDvsD6hmcMGGnCgIkcBjhaMGCmHgN66jCgoxYDWvIY4IkxgHFAkrmeU7kmfpkG4uUllXMtEE82QPs2QPs2QPs2QPs2QPsAoH0DoH0LoH0QeN3ydtnyXg38ok76MQ/gAfwf8JH8V18LQOljomBAFAyIkga4CGT7XAtEuQf4IpDri6e2BoiZA6OrAGLuxOnLQMweWXMRyPVlwQ/NAwZMNGLACJ/76ODYo5MHHl3o7EOHZ6BbjtbpG/5kfOv8P7ilbv7PX/M/rMVPqrI3XI3nd3sAAAAASUVORK5CYII=";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddLocalConfiguration();

        builder.ConfigureStructuredConsoleLogging();

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddApiHttpConventions();

        builder.AddDefaultApiDocumentation();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // 默认开启弹性（重试、熔断、超时等）
            http.AddStandardResilienceHandler();

            // 默认开启服务发现
            http.AddServiceDiscovery();
        });

        // 如需限制服务发现允许的协议，取消下面注释。
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    // 非 Development 环境下，控制台每行输出一个 JSON 对象（含日志作用域），便于日志采集。
    public static TBuilder ConfigureStructuredConsoleLogging<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (builder.Environment.IsDevelopment())
        {
            return builder;
        }

        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // 健康检查请求不纳入链路追踪
                        options.Filter = context => !IsProbePath(context.Request.Path);
                        options.EnrichWithHttpRequest = TokenQueryRedaction.RedactRequest;
                    })
                    // 如需启用 gRPC 埋点，取消下面这行注释（需要 OpenTelemetry.Instrumentation.GrpcNetClient 包）
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation(options => options.EnrichWithHttpRequestMessage = TokenQueryRedaction.RedactOutbound);
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // 如需启用 Azure Monitor 导出器，取消下面注释（需要 Azure.Monitor.OpenTelemetry.AspNetCore 包）
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // 第一阶段没有进程之外的依赖，因此同一个检查同时支撑存活与就绪两个探针。
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag, ReadyTag]);

        return builder;
    }

    // 顺序有讲究：traceId 最先，请求日志包住错误处理，确保记录到最终状态码。
    public static WebApplication UseDefaultHttpPipeline(this WebApplication app)
    {
        app.UseMiddleware<TraceIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseApiErrorHandling();

        return app;
    }

    // Swashbuckle generates the document shared by Swagger UI at /swagger and Scalar at /scalar.
    // 接口与契约说明取自 XML 注释：服务程序集提供控制器与契约，统一响应体契约在 BuildingBlocks.Http。
    public static TBuilder AddDefaultApiDocumentation<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "员工/客服的 Keycloak 访问令牌（POST /api/v1/login 获得）；粘贴访问令牌，不要带 Bearer 前缀。"
            });
            options.AddSecurityDefinition("lmsAppBearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "司机/客户的 LMS 本地访问令牌，由 POST /api/platform/{driver|customer}/v1/logins 获得；粘贴访问令牌，不要带 Bearer 前缀。"
            });
            options.OperationFilter<BearerSecurityOperationFilter>();
            options.OperationFilter<ApiErrorResponsesOperationFilter>();
            options.OperationFilter<ApiRequestBodyOperationFilter>();
            options.OperationFilter<QueryRequestParametersOperationFilter>();
            options.OperationFilter<RequestHeadersOperationFilter>();

            IncludeXmlComments(options, typeof(ApiEnvelope<>).Assembly);
            if (Assembly.GetEntryAssembly() is { } serviceAssembly)
            {
                IncludeXmlComments(options, serviceAssembly);
            }
        });

        return builder;
    }

    // XML 注释文件与程序集同名且同目录；未开启 GenerateDocumentationFile 的程序集没有该文件，跳过即可。
    private static void IncludeXmlComments(SwaggerGenOptions options, Assembly assembly)
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }

    public static WebApplication MapDefaultApiDocumentation(this WebApplication app) =>
        app.MapSwaggerUi(app.Environment.ApplicationName);

    // Keep both documentation interfaces available under the same environment restriction.
    public static WebApplication MapSwaggerUi(this WebApplication app, string documentTitle)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(ApiDocumentationPath, documentTitle);
            options.DocumentTitle = documentTitle;
            options.RoutePrefix = ApiDocumentationUiPath.TrimStart('/');
            options.HeadContent = FaviconHeadContent;
        });
        app.MapScalarApiReference(ScalarApiDocumentationUiPath, options => ConfigureScalar(options, documentTitle))
            .ExcludeFromDescription();

        return app;
    }

    private static void ConfigureScalar(ScalarOptions options, string documentTitle)
    {
        options.WithTitle(documentTitle);
        options.AddPreferredSecuritySchemes(["bearerAuth", "lmsAppBearer"]);
        options.HideModels();
        options.WithFavicon(WinsonFaviconDataUrl);
        options.AddDocument("v1", documentTitle, ApiDocumentationPath);
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // 探针仅限内部使用，不走业务统一响应体。非 Development 环境默认关闭，
        // 只有配置 HealthEndpoints:Enabled 后才开放；参见 https://aka.ms/dotnet/aspire/healthchecks。
        if (!AreHealthEndpointsEnabled(app))
        {
            return app;
        }

        // 只有标记为 "live" 的检查通过，进程才算存活
        app.MapHealthChecks(LivenessEndpointPath, new HealthCheckOptions { Predicate = r => r.Tags.Contains(LiveTag) })
            .ExcludeFromDescription();

        // 只有标记为 "ready" 的检查通过，应用启动后才会接收流量
        app.MapHealthChecks(ReadinessEndpointPath, new HealthCheckOptions { Predicate = r => r.Tags.Contains(ReadyTag) })
            .ExcludeFromDescription();

        return app;
    }

    internal static bool IsProbePath(PathString path) =>
        path.StartsWithSegments(LivenessEndpointPath) || path.StartsWithSegments(ReadinessEndpointPath);

    private static bool AreHealthEndpointsEnabled(WebApplication app) =>
        app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>(HealthEndpointsEnabledKey);
}

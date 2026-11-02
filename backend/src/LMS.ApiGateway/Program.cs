using LMS.ApiGateway.Authentication;
using LMS.ApiGateway.Cors;
using LMS.ApiGateway.Documentation;
using LMS.ApiGateway.Routing;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Results;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGatewayCors(builder.Configuration);
builder.Services.AddGatewayReverseProxy();
builder.Services.AddGatewayApiDocumentation(builder.Configuration);
builder.Services.AddGatewayLogin(builder.Configuration);

var app = builder.Build();

app.UseDefaultHttpPipeline();

app.UseCors(GatewayCorsExtensions.PolicyName);

app.MapGatewayApiDocumentation();

// 网关横幅只用于确认网关就绪，不是对外业务接口，因此不进 API 文档。
app.MapGet("/", (IHostEnvironment environment) =>
        ApiResults.Ok(new ServiceBannerResponse(environment.ApplicationName, "LMS API Gateway")))
    .WithName("getGatewayBanner")
    .ExcludeFromDescription();

app.MapReverseProxy();
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Identity.Authentication;
using LMS.BuildingBlocks.Realtime.Extensions;
using LMS.Dispatch.Api.Documentation;
using LMS.Dispatch.Api.Notifications;
using LMS.Dispatch.Api.Extensions;
using LMS.Dispatch.Application.Extensions;
using LMS.Dispatch.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddLmsIdentity(builder.Configuration);
builder.Services.AddDispatchApplication(builder.Configuration);
builder.Services.AddDispatchInfrastructure(builder.Configuration);
builder.Services.AddDispatchApi(builder.Configuration);
builder.Services.AddLmsRealtime(builder.Configuration);
builder.Services.Configure<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>(options =>
{
    options.OperationFilter<CreateShiftRequestExamples>();
    options.OperationFilter<CreateVehicleRequestExamples>();
});

var app = builder.Build();

app.UseDefaultHttpPipeline();

// 认证在统一错误处理之后：令牌缺失、无效或过期都以统一 Envelope（40100 / 40101）返回。
app.UseAuthentication();
app.UseAuthorization();

app.MapSwaggerUi("调度服务");

// 业务控制器全部要求已验证的企业身份；Identity:Authority 未配置时一律 401，不放行任何调用。
app.MapControllers();

// 实时通知 Hub：认证与授权由 Hub 上的策略强制，连接后只加入服务端确定的个人组。
app.MapHub<DriverNotificationHub>(NotificationHubRoutes.Driver);
app.MapDefaultEndpoints();

app.Run();

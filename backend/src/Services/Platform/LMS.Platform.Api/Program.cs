using LMS.BuildingBlocks.Identity.Authentication;
using LMS.Platform.Api.Extensions;
using LMS.Platform.Application.Extensions;
using LMS.Platform.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddLmsIdentity(builder.Configuration);
builder.Services.AddPlatformApplication(builder.Configuration);
builder.Services.AddPlatformInfrastructure(builder.Configuration);
builder.Services.AddPlatformApi();

var app = builder.Build();

app.UseDefaultHttpPipeline();

// 认证在统一错误处理之后：令牌缺失、无效或过期都以统一 Envelope（40100 / 40101）返回。
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultApiDocumentation();

// 业务控制器全部要求已验证的企业身份；Identity:Authority 未配置时一律 401，不放行任何调用。
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

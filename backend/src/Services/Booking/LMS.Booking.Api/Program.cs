using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Identity.Authentication;
using LMS.BuildingBlocks.Realtime.Extensions;
using LMS.Booking.Api.Notifications;
using LMS.Booking.Api.Extensions;
using LMS.Booking.Application.Extensions;
using LMS.Booking.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddLmsIdentity(builder.Configuration);
builder.Services.AddBookingApplication(builder.Configuration);
builder.Services.AddBookingInfrastructure(builder.Configuration);
builder.Services.AddBookingApi();
builder.Services.AddLmsRealtime(builder.Configuration);

var app = builder.Build();

app.UseDefaultHttpPipeline();

// 认证在统一错误处理之后：令牌缺失、无效或过期都以统一 Envelope（40100 / 40101）返回。
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultApiDocumentation();

// 业务控制器全部要求已验证的企业身份；Identity:Authority 未配置时一律 401，不放行任何调用。
app.MapControllers();

// 实时通知 Hub：认证与授权由 Hub 上的策略强制，连接后只加入服务端确定的个人组。
app.MapHub<StaffNotificationHub>(NotificationHubRoutes.Staff);
app.MapHub<CustomerNotificationHub>(NotificationHubRoutes.Customer);
app.MapDefaultEndpoints();

app.Run();

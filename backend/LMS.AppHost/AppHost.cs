using LMS.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Platform 读取 Booking 的权威每日限额，而 Booking 又通过服务发现访问 Platform（解析授权、核对物业与客户限制、文件）。
// 两者互相依赖，因此 Platform 使用固定端口直接指向 Booking，其余服务使用服务引用。
// Platform 还要调用 Dispatch / Booking 的内部接口核对司机、客户的登录凭据，因此同样用固定端口直接指向 Dispatch。
var platformApi = builder.AddProject<Projects.LMS_Platform_Api>("platform-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("Booking__BaseAddress", "http://localhost:5690")
    .WithEnvironment("Dispatch__BaseAddress", "http://localhost:5689")
    .WithEnvironment("Reporting__BaseAddress", "http://localhost:5692")
    .WithAppTokenSigning(builder.Configuration)
    .WithEnvironment("FileStorage__RootPath", "storage/files")
    .WithEnvironment("FileStorage__PublicBaseUrl", "http://localhost:5088")
    .WithEnvironment("FileStorage__SigningKey", builder.Configuration["FileStorage:SigningKey"] ?? throw new InvalidOperationException("FileStorage:SigningKey must be configured."))
    .WithEnvironment("FileStorage__MaxSizeBytes", "10485760")
    .WithEnvironment("FileStorage__AllowedContentTypes__0", "application/pdf")
    .WithEnvironment("FileStorage__AllowedContentTypes__1", "image/jpeg")
    .WithEnvironment("FileStorage__AllowedContentTypes__2", "image/png")
    .WithEnvironment("ConfigurationAssets__Environment", "Development")
    .WithEndpoint(name: "platform-api-http", port: 5688, scheme: "http");

// Dispatch 通过服务发现访问 Platform（解析调用者授权、核对物业代码），因此引用 platformApi。
// Dispatch 还要访问 Booking（待分配行程、权威指派），而 Booking 又引用 dispatchApi（司机状态），因此与 Platform 访问 Booking 一样使用固定端口直接指向 Booking。
var dispatchApi = builder.AddProject<Projects.LMS_Dispatch_Api>("dispatch-api")
    .WithReference(platformApi)
    .WithAppTokenVerification(builder.Configuration)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("Booking__BaseAddress", "http://localhost:5690")
    .WithEndpoint(name: "dispatch-api-http", port: 5689, scheme: "http");

// Booking 访问 Platform（授权、物业、客户限制、文件）和 Dispatch（司机状态）。
var bookingApi = builder.AddProject<Projects.LMS_Booking_Api>("booking-api")
    .WithReference(platformApi)
    .WithReference(dispatchApi)
    .WithAppTokenVerification(builder.Configuration)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEndpoint(name: "booking-api-http", port: 5690, scheme: "http");

// Notification 与 Reporting 同样通过服务发现访问 Platform（解析调用者授权）；Reporting 定时重算报表时还要读取 Booking 与 Dispatch 的报表来源。
var notificationApi = builder.AddProject<Projects.LMS_Notification_Api>("notification-api")
    .WithReference(platformApi)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEndpoint(name: "notification-api-http", port: 5691, scheme: "http");

var reportingApi = builder.AddProject<Projects.LMS_Reporting_Api>("reporting-api")
    .WithReference(platformApi)
    .WithReference(dispatchApi)
    .WithReference(bookingApi)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("FileStorage__RootPath", "storage/reports")
    .WithEnvironment("FileStorage__PublicBaseUrl", "http://localhost:5088")
    .WithEnvironment("FileStorage__SigningKey", builder.Configuration["FileStorage:SigningKey"] ?? throw new InvalidOperationException("FileStorage:SigningKey must be configured."))
    .WithEndpoint(name: "reporting-api-http", port: 5692, scheme: "http");

builder.AddProject<Projects.LMS_ApiGateway>("api-gateway")
    .WithReference(platformApi)
    .WithReference(dispatchApi)
    .WithReference(bookingApi)
    .WithReference(notificationApi)
    .WithReference(reportingApi)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEndpoint(name: "api-gateway-http", port: 5088, scheme: "http");

builder.Build().Run();

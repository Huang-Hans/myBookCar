using LMS.BuildingBlocks.Http.Extensions;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Authorization;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Workers;
using LMS.Platform.Application.Abstractions;

namespace LMS.Platform.Api.Extensions;

public static class PlatformApiExtensions
{
    public static IServiceCollection AddPlatformApi(this IServiceCollection services)
    {
        services.AddApiControllers();
        services.AddHttpContextAccessor();
        services.AddScoped<ICallerToken, HttpContextCallerToken>();
        services.AddScoped<ICallerAuthorizationResolver, PortalUserCallerAuthorizationResolver>();
        services.AddScoped<AppAuthenticationFlow>();
        services.AddHostedService<AuditExportWorker>();

        return services;
    }
}

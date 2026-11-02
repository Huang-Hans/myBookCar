using LMS.BuildingBlocks.Http.Extensions;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Reporting.Api.Authorization;
using LMS.Reporting.Api.Workers;
using LMS.Reporting.Application.Abstractions;

namespace LMS.Reporting.Api.Extensions;

public static class ReportingApiExtensions
{
    public static IServiceCollection AddReportingApi(this IServiceCollection services)
    {
        services.AddApiControllers();
        services.AddHttpContextAccessor();
        services.AddScoped<ICallerToken, HttpContextCallerToken>();
        services.AddScoped<ICallerAuthorizationResolver, PlatformCallerAuthorizationResolver>();
        services.AddHostedService<ReportExportWorker>();
        services.AddHostedService<ReportRowRefreshWorker>();

        return services;
    }
}

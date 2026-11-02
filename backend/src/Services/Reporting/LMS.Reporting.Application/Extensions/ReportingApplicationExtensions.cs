using LMS.Reporting.Application.Exports.Handlers;
using LMS.Reporting.Application.Exports.Options;
using LMS.Reporting.Application.Exports.Services;
using LMS.Reporting.Application.Files.Handlers;
using LMS.Reporting.Application.Files.Options;
using LMS.Reporting.Application.Files.Services;
using LMS.Reporting.Application.Idempotency.Options;
using LMS.Reporting.Application.Refresh.Handlers;
using LMS.Reporting.Application.Refresh.Options;
using LMS.Reporting.Application.Reports.Handlers;
using LMS.Reporting.Application.Reports.Options;
using LMS.Reporting.Application.Schedules.Handlers;
using LMS.Reporting.Application.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Application.Extensions;

public static class ReportingApplicationExtensions
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        AddOptions(services, configuration);
        services.AddScoped<CreateWorkflow>();
        services.AddScoped<IReportQueryHandler, ReportQueryHandler>();
        services.AddScoped<IRefreshReportRowsHandler, RefreshReportRowsHandler>();
        services.AddScoped<IReportScheduleQueryHandler, ReportScheduleQueryHandler>();
        services.AddScoped<ICreateReportScheduleHandler, CreateReportScheduleHandler>();
        services.AddScoped<IRemoveReportScheduleHandler, RemoveReportScheduleHandler>();
        AddExportHandlers(services);

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<IdempotencyOptions>, IdempotencyOptionsValidator>();
        services.AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<ReportingOptions>().Bind(configuration.GetSection(ReportingOptions.SectionName));
        services.AddOptions<ReportRefreshOptions>().Bind(configuration.GetSection(ReportRefreshOptions.SectionName));
        services.AddOptions<ExportOptions>().Bind(configuration.GetSection(ExportOptions.SectionName));
        services.AddOptions<FileStorageOptions>().Bind(configuration.GetSection(FileStorageOptions.SectionName));
    }

    private static void AddExportHandlers(IServiceCollection services)
    {
        services.AddSingleton<DownloadUrlSigner>();
        services.AddScoped<ICreateReportExportHandler, CreateReportExportHandler>();
        services.AddScoped<IReportExportQueryHandler, ReportExportQueryHandler>();
        services.AddScoped<IReportExportRunner, ReportExportRunner>();
        services.AddScoped<IDownloadFileContentHandler, DownloadFileContentHandler>();
    }
}

using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Extensions;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Auditing.Ports;
using LMS.Reporting.Application.Directory;
using LMS.Reporting.Application.Exports.Ports;
using LMS.Reporting.Application.Files.Ports;
using LMS.Reporting.Application.Idempotency.Ports;
using LMS.Reporting.Application.Refresh.Ports;
using LMS.Reporting.Application.Reports.Ports;
using LMS.Reporting.Application.Schedules.Ports;
using LMS.Reporting.Infrastructure.Exports;
using LMS.Reporting.Infrastructure.Files;
using LMS.Reporting.Infrastructure.Platform;
using LMS.Reporting.Infrastructure.Repositories;
using LMS.Reporting.Infrastructure.ServiceIdentity;
using LMS.Reporting.Infrastructure.Sources;
using LMS.Reporting.Infrastructure.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Infrastructure.Extensions;

public static class ReportingInfrastructureExtensions
{
    // 组合根注册入口：Api 通过它装配持久化、文件存储与 Platform 读取，端点代码只面向 Application 接口。
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);
        services.AddScoped<IPropertyReferenceReader, MongoPropertyReferenceReader>();

        services.AddSingleton<ITransactionRunner, MongoTransactionRunner>();
        services.AddScoped<IReportRowReader, MongoReportRowReader>();
        services.AddScoped<IReportScheduleRepository, MongoReportScheduleRepository>();
        services.AddScoped<IReportExportRepository, MongoReportExportRepository>();
        services.AddScoped<IFileMetadataRepository, MongoFileMetadataRepository>();
        services.AddScoped<IAuditRecordRepository, MongoAuditRecordRepository>();
        services.AddScoped<IIdempotencyRecordRepository, MongoIdempotencyRecordRepository>();
        services.AddSingleton<IFileContentStore, LocalDiskFileContentStore>();
        services.AddSingleton<IWorkbookWriter, XlsxWorkbookWriter>();

        services.AddOptions<PlatformClientOptions>().Bind(configuration.GetSection(PlatformClientOptions.SectionName));
        services.AddHttpClient<IPlatformDirectory, PlatformDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PlatformClientOptions>>().Value.BaseAddress));

        AddReportSources(services, configuration);

        return services;
    }

    // 报表行定时重算：以 Reporting 服务身份读取 Booking / Dispatch / Platform 的报表来源，写入 report_rows。
    private static void AddReportSources(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceSigningOptions>().Bind(configuration.GetSection(ServiceSigningOptions.SectionName));
        services.AddSingleton<IServiceTokenIssuer, ServiceTokenSigner>();
        services.AddOptions<SourceClientOptions>().Bind(configuration.GetSection(SourceClientOptions.SectionName));
        services.AddHttpClient<IBookingTripSource, BookingTripSource>((provider, client) =>
            client.BaseAddress = new Uri(SourcesOf(provider).BookingBaseAddress));
        services.AddHttpClient<IDispatchShiftSource, DispatchShiftSource>((provider, client) =>
            client.BaseAddress = new Uri(SourcesOf(provider).DispatchBaseAddress));
        services.AddHttpClient<ICancellationReasonSource, PlatformCancellationReasonSource>((provider, client) =>
            client.BaseAddress = new Uri(SourcesOf(provider).PlatformBaseAddress));
        services.AddScoped<IReportRowWriter, MongoReportRowWriter>();
        services.AddScoped<IReportRefreshLock, MongoReportRefreshLock>();
    }

    private static SourceClientOptions SourcesOf(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<SourceClientOptions>>().Value;
}

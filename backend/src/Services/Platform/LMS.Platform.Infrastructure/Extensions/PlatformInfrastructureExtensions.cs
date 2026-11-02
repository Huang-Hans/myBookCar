using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Extensions;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.AuditRecords.Ports;
using LMS.Platform.Application.Auditing.Ports;
using LMS.Platform.Application.Authorization.Ports;
using LMS.Platform.Application.Exports.Ports;
using LMS.Platform.Application.ConfigurationReleases.Ports;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Application.CustomerBlacklists.Ports;
using LMS.Platform.Application.Files.Ports;
using LMS.Platform.Application.Idempotency.Ports;
using LMS.Platform.Application.Quotas;
using LMS.Platform.Application.Locations.Ports;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Infrastructure.Configuration;
using LMS.Platform.Infrastructure.Configuration.References;
using LMS.Platform.Infrastructure.Exports;
using LMS.Platform.Infrastructure.Files;
using LMS.Platform.Infrastructure.Quotas;
using LMS.Platform.Infrastructure.Repositories;
using LMS.Platform.Infrastructure.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Infrastructure.Extensions;

public static class PlatformInfrastructureExtensions
{
    // 组合根注册入口：Api 通过它装配持久化实现，端点代码只面向 Application 接口。
    public static IServiceCollection AddPlatformInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);
        services.AddScoped<IPropertyReferenceReader, MongoPropertyReferenceReader>();

        services.AddSingleton<ITransactionRunner, MongoTransactionRunner>();
        services.AddScoped<IConfigurationReleaseRepository, MongoConfigurationReleaseRepository>();
        services.AddScoped<IActiveConfigurationRepository, MongoActiveConfigurationRepository>();
        services.AddScoped<IAuditRecordRepository, MongoAuditRecordRepository>();
        services.AddScoped<IIdempotencyRecordRepository, MongoIdempotencyRecordRepository>();
        services.AddScoped<IAuditRecordReader, MongoAuditRecordReader>();
        services.AddScoped<IPortalUserRepository, MongoPortalUserRepository>();
        services.AddSingleton<IRolePermissionRepository, MongoRolePermissionRepository>();
        services.AddScoped<ILocationRepository, MongoLocationRepository>();
        services.AddScoped<ICustomerBlacklistRepository, MongoCustomerBlacklistRepository>();
        services.AddScoped<IPropertyRepository, MongoPropertyRepository>();
        services.AddScoped<ConfigurationSnapshotStore>();
        services.AddScoped<IPropertyReferenceDirectory, PropertyReferenceDirectory>();
        services.AddOptions<ReportingClientOptions>().Bind(configuration.GetSection(ReportingClientOptions.SectionName));
        services.AddHttpClient<PropertyReferenceClient>(client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddAppAuthenticationInfrastructure(configuration);
        services.AddConfigurationAssetSources();
        services.AddScoped<IFileMetadataRepository, MongoFileMetadataRepository>();
        services.AddSingleton<IFileContentStore, LocalDiskFileContentStore>();
        services.AddScoped<IAuditExportRepository, MongoAuditExportRepository>();
        services.AddSingleton<IWorkbookWriter, XlsxWorkbookWriter>();

        services.AddOptions<BookingClientOptions>().Bind(configuration.GetSection(BookingClientOptions.SectionName));
        services.AddHttpClient<IPropertyQuotaDirectory, BookingQuotaDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<BookingClientOptions>>().Value.BaseAddress));

        return services;
    }
}

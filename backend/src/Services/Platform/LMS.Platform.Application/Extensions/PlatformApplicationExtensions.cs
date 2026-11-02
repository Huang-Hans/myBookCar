using LMS.Platform.Application.AuditRecords.Handlers;
using LMS.Platform.Application.Authentication.Handlers;
using LMS.Platform.Application.Authentication.Options;
using LMS.Platform.Application.Authentication.Services;
using LMS.Platform.Application.Authorization.Handlers;
using LMS.Platform.Application.Authorization.Options;
using LMS.Platform.Application.Authorization.Services;
using LMS.Platform.Application.Business;
using LMS.Platform.Application.Configuration.Catalogs;
using LMS.Platform.Application.Configuration.Definitions;
using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Options;
using LMS.Platform.Application.Exports.Handlers;
using LMS.Platform.Application.Exports.Options;
using LMS.Platform.Application.Exports.Services;
using LMS.Platform.Application.ConfigurationReleases.Handlers;
using LMS.Platform.Application.CustomerBlacklists.Handlers;
using LMS.Platform.Application.Files.Handlers;
using LMS.Platform.Application.Files.Options;
using LMS.Platform.Application.Files.Services;
using LMS.Platform.Application.Idempotency.Options;
using LMS.Platform.Application.Locations.Handlers;
using LMS.Platform.Application.Notifications.Handlers;
using LMS.Platform.Application.Notifications.Options;
using LMS.Platform.Application.Users.Handlers;
using LMS.Platform.Application.Users.Options;
using LMS.Platform.Application.Users.Services;
using LMS.Platform.Application.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Extensions;

public static class PlatformApplicationExtensions
{
    public static IServiceCollection AddPlatformApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        AddOptions(services, configuration);
        services.AddScoped<IPublishConfigurationReleaseHandler, PublishConfigurationReleaseHandler>();
        AddWorkflows(services);
        AddMasterDataHandlers(services);
        AddConfigurationHandlers(services);
        AddAuthorizationHandlers(services);
        AddAppAuthenticationHandlers(services);
        AddFileHandlers(services);
        AddExportHandlers(services);

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<IdempotencyOptions>, IdempotencyOptionsValidator>();
        services.AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<RolePermissionOptions>, RolePermissionOptionsValidator>();
        services.AddOptions<RolePermissionOptions>()
            .Bind(configuration.GetSection(RolePermissionOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AppAuthenticationOptions>, AppAuthenticationOptionsValidator>();
        services.AddOptions<AppAuthenticationOptions>()
            .Bind(configuration.GetSection(AppAuthenticationOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<PortalUserOptions>().Bind(configuration.GetSection(PortalUserOptions.SectionName));
        services.AddSingleton<IValidateOptions<NotificationRecipientOptions>, NotificationRecipientOptionsValidator>();
        services.AddOptions<NotificationRecipientOptions>()
            .Bind(configuration.GetSection(NotificationRecipientOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<ConfigurationAssetsOptions>().Bind(configuration.GetSection(ConfigurationAssetsOptions.SectionName));
        services.AddOptions<FileStorageOptions>().Bind(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddOptions<PlatformBusinessOptions>().Bind(configuration.GetSection(PlatformBusinessOptions.SectionName));
        services.AddOptions<ExportOptions>().Bind(configuration.GetSection(ExportOptions.SectionName));
    }

    private static void AddWorkflows(IServiceCollection services)
    {
        services.AddScoped<CreateWorkflow>();
        services.AddScoped<PatchWorkflow>();
    }

    private static void AddMasterDataHandlers(IServiceCollection services)
    {
        services.AddScoped<IPortalUserQueryHandler, PortalUserQueryHandler>();
        services.AddScoped<ICreatePortalUserHandler, CreatePortalUserHandler>();
        services.AddScoped<IUpdatePortalUserHandler, UpdatePortalUserHandler>();
        services.AddScoped<ICurrentUserHandler, CurrentUserHandler>();

        services.AddScoped<ILocationQueryHandler, LocationQueryHandler>();
        services.AddScoped<ICreateLocationHandler, CreateLocationHandler>();
        services.AddScoped<IUpdateLocationHandler, UpdateLocationHandler>();
        services.AddScoped<IReplaceLocationOrderHandler, ReplaceLocationOrderHandler>();

        services.AddScoped<ICustomerBlacklistQueryHandler, CustomerBlacklistQueryHandler>();
        services.AddScoped<ICreateCustomerBlacklistHandler, CreateCustomerBlacklistHandler>();
        services.AddScoped<IUpdateCustomerBlacklistHandler, UpdateCustomerBlacklistHandler>();

        services.AddScoped<IAuditRecordQueryHandler, AuditRecordQueryHandler>();
        services.AddSingleton<BusinessCalendar>();
        services.AddScoped<ICheckCustomerRestrictionsHandler, CheckCustomerRestrictionsHandler>();
    }

    private static void AddConfigurationHandlers(IServiceCollection services)
    {
        services.AddSingleton(PropertyDefinition.Create());
        services.AddSingleton(CancellationReasonDefinition.Create());
        services.AddSingleton(LocationCategoryDefinition.Create());
        services.AddSingleton(TravelBufferDefinition.Create());
        services.AddSingleton(NotificationTemplateDefinition.Create());
        services.AddSingleton(NotificationChannelDefinition.Create());
        services.AddSingleton(ConfigurationRegisterDefinition.Create());
        services.AddSingleton(RolePermissionReferenceDefinition.Create());
        services.AddSingleton(IntegrationReferenceDefinition.Create());
        services.AddSingleton(MembershipTierDefinition.Create());

        services.AddScoped(typeof(IConfigurationListHandler<>), typeof(ConfigurationListHandler<>));
        services.AddScoped(typeof(IConfigurationDocumentHandler<>), typeof(ConfigurationDocumentHandler<>));
        services.AddScoped<IPropertyCatalog, PropertyCatalog>();
        services.AddScoped<ILocationCategoryCatalog, LocationCategoryCatalog>();
        services.AddScoped<ICreatePropertyHandler, CreatePropertyHandler>();
        services.AddScoped<IUpdatePropertyHandler, UpdatePropertyHandler>();
        services.AddScoped<IDeletePropertyHandler, DeletePropertyHandler>();
    }

    private static void AddAuthorizationHandlers(IServiceCollection services)
    {
        services.AddSingleton<RolePermissionPolicy>();
        services.AddSingleton<CallerAccessFactory>();
        services.AddScoped<PortalUserIdentityLinker>();
        services.AddScoped<IResolveCallerHandler, ResolveCallerHandler>();
        services.AddScoped<INotificationRecipientQueryHandler, NotificationRecipientQueryHandler>();
        services.AddScoped<ILinkCallerIdentityHandler, LinkCallerIdentityHandler>();
    }

    private static void AddAppAuthenticationHandlers(IServiceCollection services)
    {
        services.AddScoped<AppSessionStore>();
        services.AddScoped<ILoginAppPrincipalHandler, LoginAppPrincipalHandler>();
        services.AddScoped<IRefreshAppSessionHandler, RefreshAppSessionHandler>();
        services.AddScoped<ILogoutAppSessionHandler, LogoutAppSessionHandler>();
    }

    private static void AddFileHandlers(IServiceCollection services)
    {
        services.AddSingleton<UploadUrlSigner>();
        services.AddScoped<IRegisterFileHandler, RegisterFileHandler>();
        services.AddScoped<IFileQueryHandler, FileQueryHandler>();
        services.AddScoped<IUploadFileContentHandler, UploadFileContentHandler>();
        services.AddSingleton<DownloadUrlSigner>();
        services.AddScoped<IDownloadFileContentHandler, DownloadFileContentHandler>();
    }

    private static void AddExportHandlers(IServiceCollection services)
    {
        services.AddScoped<ICreateAuditExportHandler, CreateAuditExportHandler>();
        services.AddScoped<IAuditExportQueryHandler, AuditExportQueryHandler>();
        services.AddScoped<IAuditExportRunner, AuditExportRunner>();
    }
}

using LMS.Platform.Application.Configuration;
using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Infrastructure.Configuration;
using LMS.Platform.Infrastructure.Configuration.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LMS.Platform.Infrastructure.Extensions;

// 每个配置区域一行注册：区域代码 + 存储文档到应用模型的映射。
internal static class ConfigurationAssetSourceExtensions
{
    private const string LoggerCategory = "LMS.Platform.ConfigurationAssets";

    public static IServiceCollection AddConfigurationAssetSources(this IServiceCollection services)
    {
        services.AddScoped<ConfigurationAssetReader>();

        services.AddList<ConfiguredProperty, ConfiguredPropertyDocument>(ConfigurationAreas.Properties, document => document.ToItem());
        services.AddList<CancellationReason, CancellationReasonDocument>(ConfigurationAreas.CancellationReasons, document => document.ToItem());
        services.AddList<LocationCategory, LocationCategoryDocument>(ConfigurationAreas.LocationCategories, document => document.ToItem());
        services.AddList<TravelBuffer, TravelBufferDocument>(ConfigurationAreas.TravelBuffers, document => document.ToItem());
        services.AddList<NotificationTemplate, NotificationTemplateDocument>(ConfigurationAreas.NotificationTemplates, document => document.ToItem());
        services.AddList<NotificationChannelConfiguration, NotificationChannelDocument>(ConfigurationAreas.NotificationChannels, document => document.ToItem());
        services.AddList<RolePermissionReference, RolePermissionReferenceDocument>(ConfigurationAreas.RolePermissionReferences, document => document.ToItem());
        services.AddList<IntegrationReference, IntegrationReferenceDocument>(ConfigurationAreas.IntegrationReferences, document => document.ToItem());
        services.AddList<ConfigurationRegisterEntry, ConfigurationRegisterEntryDocument>(ConfigurationAreas.ConfigurationRegister, document => document.ToItem());
        services.AddList<MembershipTier, MembershipTierDocument>(ConfigurationAreas.MembershipTiers, document => document.ToItem());

        services.AddDocument<LoyaltyQualificationRules, LoyaltyQualificationRulesDocument>(ConfigurationAreas.LoyaltyQualificationRules, document => document.ToModel());
        services.AddDocument<SelfBookingRules, SelfBookingRulesDocument>(ConfigurationAreas.SelfBookingRules, document => document.ToModel());
        services.AddDocument<EligibilityRules, EligibilityRulesDocument>(ConfigurationAreas.EligibilityRules, document => document.ToModel());
        services.AddDocument<CsQualificationRules, CsQualificationRulesDocument>(ConfigurationAreas.CsQualificationRules, document => document.ToModel());
        services.AddDocument<SystemParameters, SystemParametersDocument>(ConfigurationAreas.SystemParameters, document => document.ToModel());

        return services;
    }

    private static void AddList<TItem, TDocument>(this IServiceCollection services, string areaCode, Func<TDocument, TItem> map) =>
        services.AddScoped<IConfigurationListSource<TItem>>(provider => new MongoConfigurationListSource<TItem, TDocument>(
            provider.GetRequiredService<ConfigurationAssetReader>(),
            areaCode,
            map,
            provider.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory)));

    private static void AddDocument<TModel, TDocument>(this IServiceCollection services, string areaCode, Func<TDocument, TModel> map)
        where TDocument : class =>
        services.AddScoped<IConfigurationDocumentSource<TModel>>(provider => new MongoConfigurationDocumentSource<TModel, TDocument>(
            provider.GetRequiredService<ConfigurationAssetReader>(),
            areaCode,
            map,
            provider.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory)));
}

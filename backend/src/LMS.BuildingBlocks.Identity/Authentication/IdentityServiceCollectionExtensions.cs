using LMS.BuildingBlocks.Core.Security;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.BuildingBlocks.Identity.Options;
using LMS.BuildingBlocks.Identity.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LMS.BuildingBlocks.Identity.Authentication;

public static class IdentityServiceCollectionExtensions
{
    // 注册两套互相隔离的 Bearer 方案：StaffBearer（企业 Keycloak 令牌）与 LmsAppBearer（本地司机/客户令牌）。
    // 没有设置默认认证方案：每个业务入口必须通过 Authorize(Policy=...) 明确选择方案，未分类入口一律被默认策略拒绝。
    // 对应发行方尚未配置时，该方案注册为永不认证的处理器，业务路由因此一律返回 401。
    public static IServiceCollection AddLmsIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<IdentityOptions>, IdentityOptionsValidator>();
        services.AddOptions<IdentityOptions>()
            .Bind(configuration.GetSection(IdentityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AppIdentityOptions>, AppIdentityOptionsValidator>();
        services.AddOptions<AppIdentityOptions>()
            .Bind(configuration.GetSection(AppIdentityOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ServiceIdentityOptions>, ServiceIdentityOptionsValidator>();
        services.AddOptions<ServiceIdentityOptions>()
            .Bind(configuration.GetSection(ServiceIdentityOptions.SectionName))
            .ValidateOnStart();

        var staffOptions = configuration.GetSection(IdentityOptions.SectionName).Get<IdentityOptions>() ?? new IdentityOptions();
        var appOptions = configuration.GetSection(AppIdentityOptions.SectionName).Get<AppIdentityOptions>() ?? new AppIdentityOptions();
        var serviceOptions = configuration.GetSection(ServiceIdentityOptions.SectionName).Get<ServiceIdentityOptions>() ?? new ServiceIdentityOptions();
        var authentication = services.AddAuthentication(options =>
        {
            options.DefaultChallengeScheme = AuthenticationSchemes.StaffBearer;
            options.DefaultForbidScheme = AuthenticationSchemes.StaffBearer;
        });
        AddStaffBearer(authentication, staffOptions);
        AddLmsAppBearer(authentication, appOptions);
        AddServiceBearer(authentication, serviceOptions);

        services.AddLmsAuthorizationPolicies();
        services.TryAddSingleton<ICredentialHasher, AspNetCredentialHasher>();
        services.TryAddScoped<IPrincipalRecordVerifier, TokenOnlyPrincipalRecordVerifier>();
        services.TryAddScoped<IPrincipalAuthorizationResolver, AppPrincipalAuthorizationResolver>();

        return services;
    }

    private static void AddStaffBearer(AuthenticationBuilder authentication, IdentityOptions options)
    {
        if (options.IsConfigured)
        {
            authentication.AddJwtBearer(AuthenticationSchemes.StaffBearer, bearer => ConfigureStaffBearer(bearer, options));
            return;
        }

        authentication.AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>(AuthenticationSchemes.StaffBearer, _ => { });
    }

    private static void AddLmsAppBearer(AuthenticationBuilder authentication, AppIdentityOptions options)
    {
        if (options.IsConfigured)
        {
            authentication.AddJwtBearer(AuthenticationSchemes.LmsAppBearer, bearer => AppTokenValidation.Configure(bearer, options));
            return;
        }

        authentication.AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>(AuthenticationSchemes.LmsAppBearer, _ => { });
    }

    private static void AddServiceBearer(AuthenticationBuilder authentication, ServiceIdentityOptions options)
    {
        if (options.IsConfigured)
        {
            authentication.AddJwtBearer(AuthenticationSchemes.ServiceBearer, bearer => ServiceTokenValidation.Configure(bearer, options));
            return;
        }

        authentication.AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>(AuthenticationSchemes.ServiceBearer, _ => { });
    }

    private static void ConfigureStaffBearer(JwtBearerOptions bearer, IdentityOptions options)
    {
        bearer.Authority = options.Authority;
        bearer.Audience = options.Audience;
        bearer.RequireHttpsMetadata = options.RequireHttpsMetadata;
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true
        };
        bearer.Events = new JwtBearerEvents
        {
            OnMessageReceived = HubAccessTokenReader.OnMessageReceivedAsync,
            OnTokenValidated = context => AuthorizedPartyValidator.ValidateAsync(context, options),
            OnChallenge = JwtBearerChallengeHandler.OnChallengeAsync
        };
    }
}

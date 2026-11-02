using LMS.Platform.Application.Authentication.Ports;
using LMS.Platform.Infrastructure.Authentication;
using LMS.Platform.Infrastructure.Quotas;
using LMS.Platform.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Infrastructure.Extensions;

internal static class AppAuthenticationInfrastructureExtensions
{
    // 司机/客户本地认证：刷新凭据存储、RS256 签发、失败限制，以及对 Dispatch / Booking 的内部凭据核对客户端。
    public static IServiceCollection AddAppAuthenticationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<AppSigningOptions>, AppSigningOptionsValidator>();
        services.AddOptions<AppSigningOptions>().Bind(configuration.GetSection(AppSigningOptions.SectionName)).ValidateOnStart();
        services.AddOptions<DispatchClientOptions>().Bind(configuration.GetSection(DispatchClientOptions.SectionName));

        services.AddScoped<IAppRefreshTokenRepository, MongoAppRefreshTokenRepository>();
        services.AddSingleton<AppTokenSigner>();
        services.AddSingleton<IAppAccessTokenIssuer>(provider => provider.GetRequiredService<AppTokenSigner>());
        services.AddSingleton<ILoginAttemptThrottle, InMemoryLoginAttemptThrottle>();
        services.AddHttpClient<DispatchCredentialClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<DispatchClientOptions>>().Value.BaseAddress));
        services.AddHttpClient<BookingCredentialClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<BookingClientOptions>>().Value.BaseAddress));
        services.AddScoped<IAppCredentialVerifier, AppCredentialVerifier>();

        return services;
    }
}

using LMS.ApiGateway.Integrations.Keycloak;
using LMS.ApiGateway.Integrations.Platform;
using LMS.ApiGateway.Options;
using LMS.BuildingBlocks.Http.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace LMS.ApiGateway.Authentication;

internal static class GatewayLoginExtensions
{
    public static IServiceCollection AddGatewayLogin(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApiControllers();
        services.AddOptions<GatewayLoginOptions>()
            .Bind(configuration.GetSection(GatewayLoginOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(HasValidTokenEndpoint, "Login:TokenEndpoint must be an absolute HTTP or HTTPS URL.")
            .ValidateOnStart();
        services.AddHttpClient<IKeycloakLoginClient, KeycloakLoginClient>((provider, client) =>
            client.Timeout = TimeSpan.FromSeconds(provider.GetRequiredService<IOptions<GatewayLoginOptions>>().Value.TimeoutSeconds))
            .ConfigureAdditionalHttpMessageHandlers(RemoveLoginResilienceHandlers)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        AddPlatformIdentityLinkClient(services, configuration);
        services.AddScoped<IGatewayLoginService, GatewayLoginService>();

        return services;
    }

    // Login waits for this check, so it shares the login timeout and is not retried either.
    private static void AddPlatformIdentityLinkClient(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GatewayPlatformOptions>()
            .Bind(configuration.GetSection(GatewayPlatformOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient<IPlatformIdentityLinkClient, PlatformIdentityLinkClient>(ConfigurePlatformClient)
            .ConfigureAdditionalHttpMessageHandlers(RemoveLoginResilienceHandlers);
    }

    private static void ConfigurePlatformClient(IServiceProvider provider, HttpClient client)
    {
        client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<GatewayPlatformOptions>>().Value.BaseAddress);
        client.Timeout = TimeSpan.FromSeconds(provider.GetRequiredService<IOptions<GatewayLoginOptions>>().Value.TimeoutSeconds);
    }

    // Login requests must not inherit automatic retries from the shared HTTP defaults.
    private static void RemoveLoginResilienceHandlers(IList<DelegatingHandler> handlers, IServiceProvider provider)
    {
        foreach (var handler in handlers.OfType<ResilienceHandler>().ToArray())
        {
            handlers.Remove(handler);
        }
    }

    // An empty endpoint disables login without preventing other gateway routes from starting.
    private static bool HasValidTokenEndpoint(GatewayLoginOptions options) =>
        !options.IsConfigured || IsHttpEndpoint(options.TokenEndpoint);

    private static bool IsHttpEndpoint(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}

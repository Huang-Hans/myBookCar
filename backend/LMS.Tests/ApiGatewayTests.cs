using Microsoft.Extensions.Logging;

namespace LMS.Tests;

public class ApiGatewayTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task GatewayServesBanner()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LMS_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("api-gateway");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api-gateway", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var gatewayResponse = await httpClient.GetAsync("/", cancellationToken);

        // Assert：网关横幅接口返回统一信封（200 / 20000）。
        Assert.Equal(HttpStatusCode.OK, gatewayResponse.StatusCode);
    }
}

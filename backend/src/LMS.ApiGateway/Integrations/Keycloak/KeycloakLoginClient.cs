using System.Net;
using System.Text.Json;
using LMS.ApiGateway.Contracts.Authentication;
using LMS.ApiGateway.Integrations.Keycloak.Contracts;
using LMS.ApiGateway.Options;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using Microsoft.Extensions.Options;

namespace LMS.ApiGateway.Integrations.Keycloak;

internal sealed class KeycloakLoginClient(HttpClient client, IOptions<GatewayLoginOptions> options) : IKeycloakLoginClient
{
    private static readonly ApiError BadGateway = ApiErrors.FromStatusCode(StatusCodes.Status502BadGateway);
    private static readonly ApiError GatewayTimeout = ApiErrors.FromStatusCode(StatusCodes.Status504GatewayTimeout);

    public Task<LoginResult> LoginAsync(LoginRequest request, string traceId, CancellationToken cancellationToken) =>
        RequestTokensAsync(
            new Dictionary<string, string>
            {
                [KeycloakTokenFormFields.GrantType] = KeycloakGrantTypes.Password,
                [KeycloakTokenFormFields.ClientId] = options.Value.ClientId,
                [KeycloakTokenFormFields.Username] = request.Username,
                [KeycloakTokenFormFields.Password] = request.Password
            },
            traceId,
            cancellationToken);

    public Task<LoginResult> RefreshAsync(RefreshTokenRequest request, string traceId, CancellationToken cancellationToken) =>
        RequestTokensAsync(
            new Dictionary<string, string>
            {
                [KeycloakTokenFormFields.GrantType] = KeycloakGrantTypes.RefreshToken,
                [KeycloakTokenFormFields.ClientId] = options.Value.ClientId,
                [KeycloakTokenFormFields.RefreshToken] = request.RefreshToken
            },
            traceId,
            cancellationToken);

    private async Task<LoginResult> RequestTokensAsync(
        Dictionary<string, string> form,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            return LoginResult.Failure(ApiErrors.ServiceUnavailable);
        }

        try
        {
            return await SendAsync(form, traceId, cancellationToken);
        }
        catch (Exception exception) when (IsUpstreamFailure(exception, cancellationToken))
        {
            return LoginResult.Failure(exception is OperationCanceledException ? GatewayTimeout : BadGateway);
        }
    }

    private async Task<LoginResult> SendAsync(Dictionary<string, string> form, string traceId, CancellationToken cancellationToken)
    {
        using var message = CreateRequest(form, traceId);
        using var response = await client.SendAsync(message, cancellationToken);

        return response.IsSuccessStatusCode
            ? await ReadTokensAsync(response.Content, cancellationToken)
            : await ReadFailureAsync(response, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(Dictionary<string, string> form, string traceId)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, options.Value.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        message.Headers.Add(ApiHeaderNames.TraceId, traceId);

        return message;
    }

    private static async Task<LoginResult> ReadTokensAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var token = await content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken);
        if (token is null || !HasValidAccessToken(token))
        {
            return LoginResult.Failure(BadGateway);
        }

        return LoginResult.Success(new LoginResponse(token.AccessToken, token.ExpiresIn, token.TokenType,
            token.RefreshToken, token.RefreshExpiresIn, token.Scope, token.SessionState, token.NotBeforePolicy));
    }

    private static async Task<LoginResult> ReadFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            var error = await response.Content.ReadFromJsonAsync<KeycloakErrorResponse>(cancellationToken);
            return LoginResult.Failure(error?.Error == KeycloakErrorCodes.InvalidGrant ? ApiErrors.NotAuthenticated : BadGateway);
        }

        return LoginResult.Failure(response.StatusCode == HttpStatusCode.ServiceUnavailable ? ApiErrors.ServiceUnavailable : BadGateway);
    }

    private static bool HasValidAccessToken(KeycloakTokenResponse token) =>
        !string.IsNullOrWhiteSpace(token.AccessToken) && !string.IsNullOrWhiteSpace(token.TokenType) && token.ExpiresIn > 0;

    private static bool IsUpstreamFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or OperationCanceledException or JsonException && !cancellationToken.IsCancellationRequested;
}

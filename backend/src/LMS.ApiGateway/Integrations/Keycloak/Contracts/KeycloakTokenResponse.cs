using System.Text.Json.Serialization;

namespace LMS.ApiGateway.Integrations.Keycloak.Contracts;

internal sealed class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public required long ExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("refresh_expires_in")]
    public long? RefreshExpiresIn { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("session_state")]
    public string? SessionState { get; init; }

    [JsonPropertyName("not-before-policy")]
    public long? NotBeforePolicy { get; init; }
}

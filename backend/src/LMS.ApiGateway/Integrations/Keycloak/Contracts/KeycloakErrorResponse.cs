using System.Text.Json.Serialization;

namespace LMS.ApiGateway.Integrations.Keycloak.Contracts;

internal sealed class KeycloakErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

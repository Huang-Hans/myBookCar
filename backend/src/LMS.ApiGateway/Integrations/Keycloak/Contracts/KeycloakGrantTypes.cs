namespace LMS.ApiGateway.Integrations.Keycloak.Contracts;

// OAuth 2.0 grant_type values the gateway sends to the token endpoint.
internal static class KeycloakGrantTypes
{
    public const string Password = "password";
    public const string RefreshToken = "refresh_token";
}

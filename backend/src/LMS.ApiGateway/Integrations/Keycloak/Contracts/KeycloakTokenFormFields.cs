namespace LMS.ApiGateway.Integrations.Keycloak.Contracts;

// Form field names of the token endpoint request (RFC 6749).
internal static class KeycloakTokenFormFields
{
    public const string GrantType = "grant_type";
    public const string ClientId = "client_id";
    public const string Username = "username";
    public const string Password = "password";
    public const string RefreshToken = "refresh_token";
}

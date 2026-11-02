namespace LMS.ApiGateway.Integrations.Keycloak.Contracts;

// Error codes of the token endpoint response (RFC 6749) the gateway reacts to.
internal static class KeycloakErrorCodes
{
    public const string InvalidGrant = "invalid_grant";
}

using System.ComponentModel.DataAnnotations;

namespace LMS.ApiGateway.Options;

public sealed class GatewayLoginOptions
{
    public const string SectionName = "Login";

    public string TokenEndpoint { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = "lms-api-client";

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 10;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(TokenEndpoint);
}

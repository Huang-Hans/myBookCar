using System.ComponentModel.DataAnnotations;

namespace LMS.ApiGateway.Options;

// Platform address used by login to check and link the signed-in subject; resolved through service discovery.
public sealed class GatewayPlatformOptions
{
    public const string SectionName = "Platform";

    [Required]
    public string BaseAddress { get; init; } = "http://platform-api";
}

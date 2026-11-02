using LMS.ApiGateway.Options;
using Microsoft.Extensions.Options;

namespace LMS.ApiGateway.Configuration;

internal sealed class GatewayCorsOptionsValidator : IValidateOptions<GatewayCorsOptions>
{
    public ValidateOptionsResult Validate(string? name, GatewayCorsOptions options)
    {
        var invalidOrigins = options.AllowedOrigins.Where(origin => !HttpOriginRules.IsAllowedOrigin(origin)).ToArray();
        if (invalidOrigins.Length == 0)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            $"{GatewayCorsOptions.SectionName}:{nameof(GatewayCorsOptions.AllowedOrigins)} must contain only " +
            $"http or https origins, optionally with a *. subdomain wildcard, without a path or trailing slash. Invalid: {string.Join(", ", invalidOrigins)}");
    }
}

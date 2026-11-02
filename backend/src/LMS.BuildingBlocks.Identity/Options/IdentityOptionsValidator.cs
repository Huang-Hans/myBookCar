using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Identity.Options;

internal sealed partial class IdentityOptionsValidator : IValidateOptions<IdentityOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentityOptions options)
    {
        var failures = new List<string>();
        Require(failures, ProviderCodePattern().IsMatch(options.ProviderCode), nameof(IdentityOptions.ProviderCode),
            "must be uppercase letters, digits or underscores and start with a letter");
        Require(failures, !string.IsNullOrWhiteSpace(options.SubjectClaimType), nameof(IdentityOptions.SubjectClaimType),
            "must not be blank");
        Require(failures, !string.IsNullOrWhiteSpace(options.EmailClaimType), nameof(IdentityOptions.EmailClaimType),
            "must not be blank");
        if (options.IsConfigured)
        {
            ValidateConfiguredAuthority(failures, options);
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateConfiguredAuthority(List<string> failures, IdentityOptions options)
    {
        var isAbsoluteUri = Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority);
        Require(failures, isAbsoluteUri, nameof(IdentityOptions.Authority), "must be an absolute URI");
        Require(failures, !isAbsoluteUri || !options.RequireHttpsMetadata || authority!.Scheme == Uri.UriSchemeHttps,
            nameof(IdentityOptions.Authority), "must use https unless RequireHttpsMetadata is disabled");
        Require(failures, !string.IsNullOrWhiteSpace(options.Audience), nameof(IdentityOptions.Audience),
            "must not be blank when Authority is configured");
        Require(failures, options.AllowedClients.Any(client => !string.IsNullOrWhiteSpace(client)),
            nameof(IdentityOptions.AllowedClients), "must list at least one client when Authority is configured");
    }

    private static void Require(List<string> failures, bool isValid, string setting, string requirement)
    {
        if (!isValid)
        {
            failures.Add($"{IdentityOptions.SectionName}:{setting} {requirement}.");
        }
    }

    [GeneratedRegex("^[A-Z][A-Z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ProviderCodePattern();
}

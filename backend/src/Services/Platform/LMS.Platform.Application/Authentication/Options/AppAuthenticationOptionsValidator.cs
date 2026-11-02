using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Authentication.Options;

internal sealed class AppAuthenticationOptionsValidator : IValidateOptions<AppAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, AppAuthenticationOptions options)
    {
        var failures = new List<string>();
        Require(failures, options.AccessTokenMinutes > 0, nameof(AppAuthenticationOptions.AccessTokenMinutes));
        Require(failures, options.RefreshTokenDays > 0, nameof(AppAuthenticationOptions.RefreshTokenDays));
        Require(failures, options.MaxFailedLogins > 0, nameof(AppAuthenticationOptions.MaxFailedLogins));
        Require(failures, options.FailureWindowMinutes > 0, nameof(AppAuthenticationOptions.FailureWindowMinutes));

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(List<string> failures, bool isValid, string setting)
    {
        if (!isValid)
        {
            failures.Add($"{AppAuthenticationOptions.SectionName}:{setting} must be greater than zero.");
        }
    }
}

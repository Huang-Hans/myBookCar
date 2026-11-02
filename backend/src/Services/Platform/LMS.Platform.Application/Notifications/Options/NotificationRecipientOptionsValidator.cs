using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Domain.Users;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Notifications.Options;

internal sealed class NotificationRecipientOptionsValidator : IValidateOptions<NotificationRecipientOptions>
{
    private const int MaximumRecipientLimit = 5000;

    public ValidateOptionsResult Validate(string? name, NotificationRecipientOptions options)
    {
        var failures = options.Roles
            .Where(role => !WireEnum.TryParse<PortalRole>(role, out _))
            .Select(role => $"{NotificationRecipientOptions.SectionName}:Roles contains unknown role '{role}'.")
            .ToList();
        if (options.MaxRecipients is < 1 or > MaximumRecipientLimit)
        {
            failures.Add($"{NotificationRecipientOptions.SectionName}:MaxRecipients must be between 1 and {MaximumRecipientLimit}.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

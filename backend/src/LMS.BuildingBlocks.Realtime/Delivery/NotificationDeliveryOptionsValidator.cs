using LMS.BuildingBlocks.Core.Notifications.Delivery;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Realtime.Delivery;

internal sealed class NotificationDeliveryOptionsValidator : IValidateOptions<NotificationDeliveryOptions>
{
    public ValidateOptionsResult Validate(string? name, NotificationDeliveryOptions options)
    {
        var failures = new List<string>();
        Require(failures, options.PollSeconds >= 1, nameof(options.PollSeconds));
        Require(failures, options.BatchSize is >= 1 and <= 500, nameof(options.BatchSize));
        Require(failures, options.LeaseSeconds >= 5, nameof(options.LeaseSeconds));
        Require(failures, options.MaxAttempts >= 1, nameof(options.MaxAttempts));
        Require(failures, options.RetryBaseSeconds >= 1, nameof(options.RetryBaseSeconds));
        Require(failures, options.RetryMaxSeconds >= options.RetryBaseSeconds, nameof(options.RetryMaxSeconds));
        Require(failures, options.RetentionDays >= 1, nameof(options.RetentionDays));
        Require(failures, options.ReverifySeconds >= 10, nameof(options.ReverifySeconds));
        Require(failures, options.GuardIntervalSeconds >= 1, nameof(options.GuardIntervalSeconds));

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(List<string> failures, bool isValid, string setting)
    {
        if (!isValid)
        {
            failures.Add($"{NotificationDeliveryOptions.SectionName}:{setting} is out of range.");
        }
    }
}

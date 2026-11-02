using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Idempotency.Options;

internal sealed class IdempotencyOptionsValidator : IValidateOptions<IdempotencyOptions>
{
    private const int MaxRetentionHours = 24 * 30;

    public ValidateOptionsResult Validate(string? name, IdempotencyOptions options) =>
        options.RetentionHours is >= 1 and <= MaxRetentionHours
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{IdempotencyOptions.SectionName}:{nameof(IdempotencyOptions.RetentionHours)} must be between 1 and {MaxRetentionHours}.");
}

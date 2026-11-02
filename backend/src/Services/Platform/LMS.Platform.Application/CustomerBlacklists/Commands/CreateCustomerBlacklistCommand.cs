using System.Globalization;
using System.Text.Json;
using LMS.Platform.Application.Abstractions;

namespace LMS.Platform.Application.CustomerBlacklists.Commands;

public sealed record CreateCustomerBlacklistCommand(
    string CustomerName,
    string MemberId,
    string Reason,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate,
    bool IsActive,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            CustomerName,
            MemberId.ToLowerInvariant(),
            Reason,
            EffectiveDate.ToString("O", CultureInfo.InvariantCulture),
            ExpiryDate.ToString("O", CultureInfo.InvariantCulture),
            IsActive
        });
}

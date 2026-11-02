using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Domain.CustomerBlacklists;

public sealed record CustomerBlacklistChanges(
    Optional<string> CustomerName,
    Optional<string> MemberId,
    Optional<string> Reason,
    Optional<DateOnly> EffectiveDate,
    Optional<DateOnly> ExpiryDate,
    Optional<bool> IsActive)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(
            ("customerName", CustomerName.IsSet),
            ("memberId", MemberId.IsSet),
            ("reason", Reason.IsSet),
            ("effectiveDate", EffectiveDate.IsSet),
            ("expiryDate", ExpiryDate.IsSet),
            ("isActive", IsActive.IsSet));
}

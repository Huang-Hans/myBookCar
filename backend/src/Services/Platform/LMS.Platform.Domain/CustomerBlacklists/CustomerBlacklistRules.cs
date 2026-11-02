using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Platform.Domain.CustomerBlacklists;

public static class CustomerBlacklistRules
{
    public static IReadOnlyList<FieldIssue> Validate(CustomerBlacklist blacklist) =>
        blacklist.ExpiryDate < blacklist.EffectiveDate
            ? [new FieldIssue("expiryDate", "Must not be earlier than effectiveDate")]
            : [];
}

using System.Globalization;
using LMS.Platform.Api.Contracts.CustomerBlacklists;
using LMS.Platform.Domain.CustomerBlacklists;

namespace LMS.Platform.Api.Mapping.CustomerBlacklists;

internal static class CustomerBlacklistViewMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static CustomerBlacklistView ToView(CustomerBlacklist blacklist) => new(
        blacklist.BlacklistId,
        blacklist.CustomerName,
        blacklist.MemberId,
        blacklist.Reason,
        blacklist.EffectiveDate.ToString(DateFormat, CultureInfo.InvariantCulture),
        blacklist.ExpiryDate.ToString(DateFormat, CultureInfo.InvariantCulture),
        blacklist.IsActive,
        blacklist.Version);
}

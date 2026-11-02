using LMS.Platform.Application.Business;
using LMS.Platform.Application.CustomerBlacklists.Models;
using LMS.Platform.Application.CustomerBlacklists.Ports;

namespace LMS.Platform.Application.CustomerBlacklists.Handlers;

internal sealed class CheckCustomerRestrictionsHandler(
    ICustomerBlacklistRepository blacklists,
    BusinessCalendar calendar) : ICheckCustomerRestrictionsHandler
{
    public async Task<IReadOnlyList<CustomerRestriction>> HandleAsync(
        IReadOnlyList<string> memberIds,
        CancellationToken cancellationToken)
    {
        if (memberIds.Count == 0)
        {
            return [];
        }

        var active = await blacklists.FindActiveByMemberIdsAsync(memberIds, calendar.Today(), cancellationToken);
        var requested = memberIds.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(id => id, id => id, StringComparer.OrdinalIgnoreCase);

        return active.Select(item => new CustomerRestriction(requested[item.MemberId], item.Reason)).ToList();
    }
}

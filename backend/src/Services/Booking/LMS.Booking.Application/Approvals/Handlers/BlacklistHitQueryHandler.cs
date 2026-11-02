using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Handlers;

// 当前待审核队列中的限制命中：由 Booking 的队列、Platform 的客户限制和 Dispatch 的司机状态实时组合，没有 hit 集合。
internal sealed class BlacklistHitQueryHandler(
    IApprovalRepository approvals,
    IPlatformDirectory platform,
    IDriverDirectory drivers) : IBlacklistHitQueryHandler
{
    private const int PageSize = 200;
    private static readonly IReadOnlyList<SortTerm> QueueOrder = [new("pickupAt", false), new("approvalId", false)];

    public async Task<OperationResult<IReadOnlyList<BlacklistHit>>> HandleAsync(
        PropertyScope scope,
        string? propertyId,
        CancellationToken cancellationToken)
    {
        if (propertyId is not null && !scope.Allows(propertyId))
        {
            return OperationResult<IReadOnlyList<BlacklistHit>>.Failed(OperationOutcome.Forbidden);
        }

        var pending = await LoadPendingAsync(scope, propertyId, cancellationToken);
        var restrictions = await platform.CheckCustomersAsync(
            pending.Select(approval => approval.Guest.GuestId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), cancellationToken);
        var statuses = await drivers.GetStatusesAsync(
            pending.Select(approval => approval.AssignedDriverId).OfType<string>().Distinct(StringComparer.Ordinal).ToList(), cancellationToken);
        if (!restrictions.IsSuccess || !statuses.IsSuccess)
        {
            return OperationResult<IReadOnlyList<BlacklistHit>>.Failed(OperationOutcome.SourceUnavailable);
        }

        IReadOnlyList<BlacklistHit> hits = pending.SelectMany(approval => HitsOf(approval, restrictions.Value!, statuses.Value!)).ToList();
        return OperationResult<IReadOnlyList<BlacklistHit>>.Succeeded(hits);
    }

    private async Task<List<CrossBorderApproval>> LoadPendingAsync(PropertyScope scope, string? propertyId, CancellationToken cancellationToken)
    {
        var pending = new List<CrossBorderApproval>();
        for (var page = 1; ; page++)
        {
            var query = new ListApprovalsQuery(new ListQuery(page, PageSize, string.Empty, QueueOrder), scope, propertyId, ApprovalStatus.Pending, null, null);
            var result = await approvals.ListAsync(query, cancellationToken);
            pending.AddRange(result.Items);
            if (pending.Count >= result.TotalItems || result.Items.Count == 0)
            {
                return pending;
            }
        }
    }

    private static IEnumerable<BlacklistHit> HitsOf(
        CrossBorderApproval approval,
        IReadOnlyList<CustomerRestrictionInfo> restrictions,
        IReadOnlyList<DriverStatusSnapshot> statuses)
    {
        var customer = restrictions.FirstOrDefault(item => string.Equals(item.MemberId, approval.Guest.GuestId, StringComparison.OrdinalIgnoreCase));
        if (customer is not null)
        {
            yield return new BlacklistHit(approval.ApprovalId, approval.TripReference, HitTargetType.Customer, approval.Guest.GuestId, customer.Reason, approval.PropertyId);
        }

        var driver = statuses.FirstOrDefault(item => item.DriverId == approval.AssignedDriverId);
        if (driver is not null && driver.Status != DirectoryDriverStatus.Active)
        {
            yield return new BlacklistHit(approval.ApprovalId, approval.TripReference, HitTargetType.Driver, driver.DriverId, driver.RestrictionReason, approval.PropertyId);
        }
    }
}

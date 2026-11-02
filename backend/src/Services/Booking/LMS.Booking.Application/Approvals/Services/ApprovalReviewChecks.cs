using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Directory;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Services;

// 批准前的检查：四项材料已核验、客户当前没有有效限制、指派的司机是 Active。
// 客户限制和司机状态读取自其所有者（Platform / Dispatch），读取时刻之后可能变化——跨域事实不是强一致的。
internal sealed class ApprovalReviewChecks(IPlatformDirectory platform, IDriverDirectory drivers)
{
    public async Task<ReviewCheckResult> CheckAsync(
        IReadOnlyList<CrossBorderApproval> approvals,
        Func<int, string> fieldPrefixOf,
        CancellationToken cancellationToken)
    {
        var restrictions = await platform.CheckCustomersAsync(DistinctGuestIds(approvals), cancellationToken);
        var driverIds = approvals.Select(approval => approval.AssignedDriverId).OfType<string>().Distinct(StringComparer.Ordinal).ToList();
        var statuses = await drivers.GetStatusesAsync(driverIds, cancellationToken);
        if (!restrictions.IsSuccess || !statuses.IsSuccess)
        {
            return ReviewCheckResult.Unavailable;
        }

        var issues = new List<FieldIssue>();
        for (var index = 0; index < approvals.Count; index++)
        {
            issues.AddRange(IssuesOf(approvals[index], fieldPrefixOf(index), restrictions.Value!, statuses.Value!));
        }

        return new ReviewCheckResult(true, issues);
    }

    private static List<string> DistinctGuestIds(IReadOnlyList<CrossBorderApproval> approvals) =>
        approvals.Select(approval => approval.Guest.GuestId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static IEnumerable<FieldIssue> IssuesOf(
        CrossBorderApproval approval,
        string prefix,
        IReadOnlyList<CustomerRestrictionInfo> restrictions,
        IReadOnlyList<DriverStatusSnapshot> statuses)
    {
        if (!approval.AreAllDocumentsVerified)
        {
            yield return new FieldIssue($"{prefix}documents", "All four documents must be verified before approval");
        }

        if (restrictions.Any(restriction => string.Equals(restriction.MemberId, approval.Guest.GuestId, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new FieldIssue($"{prefix}guest.guestId", "The customer has an active restriction");
        }

        var driver = statuses.FirstOrDefault(status => status.DriverId == approval.AssignedDriverId);
        if (driver is null || driver.Status != DirectoryDriverStatus.Active)
        {
            yield return new FieldIssue($"{prefix}assignedDriverId", "An active assigned driver is required before approval");
        }
    }
}

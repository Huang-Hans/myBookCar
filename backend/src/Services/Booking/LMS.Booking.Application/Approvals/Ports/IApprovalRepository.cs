using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Ports;

public interface IApprovalRepository
{
    Task<CrossBorderApproval?> FindVisibleAsync(string approvalId, PropertyScope scope, CancellationToken cancellationToken);

    Task<CrossBorderApproval?> FindVisibleAsync(
        string approvalId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken);

    Task<CrossBorderApproval?> FindByIdAsync(string approvalId, CancellationToken cancellationToken);

    // 不可见或不存在的标识不出现在结果中。
    Task<IReadOnlyList<CrossBorderApproval>> FindVisibleByIdsAsync(
        IReadOnlyList<string> approvalIds,
        PropertyScope scope,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CrossBorderApproval>> FindVisibleByIdsAsync(
        IReadOnlyList<string> approvalIds,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CrossBorderApproval>> FindByIdsAsync(IReadOnlyList<string> approvalIds, CancellationToken cancellationToken);

    Task<PageResult<CrossBorderApproval>> ListAsync(ListApprovalsQuery query, CancellationToken cancellationToken);

    // 授权范围及筛选内的待审核数量，以及最早取货的待审核记录的阶段标签（没有待审核记录时为空）。
    Task<ApprovalSummary> SummarizeAsync(ListApprovalsQuery query, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(
        CrossBorderApproval updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Ports;

public interface IQualificationRepository
{
    Task<QualificationReference?> FindByIdAsync(string qualificationId, CancellationToken cancellationToken);

    Task<QualificationReference?> FindVisibleAsync(string qualificationId, PropertyScope scope, CancellationToken cancellationToken);

    Task<QualificationReference?> FindVisibleAsync(
        string qualificationId,
        PropertyScope scope,
        ITransactionContext context,
        CancellationToken cancellationToken);

    Task<PageResult<QualificationReference>> ListAsync(ListQualificationsQuery query, CancellationToken cancellationToken);

    // 同宾客+物业当前处于待经理复核（MANAGER_APPROVAL_REQUIRED）的记录；用于避免重复登记。
    Task<QualificationReference?> FindPendingByGuestAsync(string guestId, string propertyId, CancellationToken cancellationToken);

    // 同宾客+物业未决的复审申请：已升级（ESCALATED）优先，其次待复核（MANAGER_APPROVAL_REQUIRED）；没有为 null。
    Task<QualificationReference?> FindOpenRequestByGuestAsync(string guestId, string propertyId, CancellationToken cancellationToken);

    Task InsertAsync(QualificationReference reference, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(
        QualificationReference updated,
        long expectedVersion,
        ITransactionContext context,
        CancellationToken cancellationToken);
}

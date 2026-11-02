using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

public interface IApproveQualificationExceptionHandler
{
    // ESCALATED → APPROVED（仅经理授权主体）。不提供拒绝例外。
    Task<OperationResult<QualificationReference>> HandleAsync(QualificationExceptionCommand command, CancellationToken cancellationToken);
}

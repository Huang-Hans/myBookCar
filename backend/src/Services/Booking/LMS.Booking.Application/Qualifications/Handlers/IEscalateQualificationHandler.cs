using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

public interface IEscalateQualificationHandler
{
    // MANAGER_APPROVAL_REQUIRED → ESCALATED。
    Task<OperationResult<QualificationReference>> HandleAsync(QualificationExceptionCommand command, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Application.Qualifications.Services;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

internal sealed class RejectQualificationExceptionHandler(QualificationExceptionExecutor executor) : IRejectQualificationExceptionHandler
{
    public Task<OperationResult<QualificationReference>> HandleAsync(QualificationExceptionCommand command, CancellationToken cancellationToken) =>
        executor.ExecuteAsync(command, QualificationTransition.Rejection, cancellationToken);
}

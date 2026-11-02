using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

public interface IRejectQualificationExceptionHandler
{
    // ESCALATED → REJECTED（仅经理授权主体）。拒绝只记录复审结果，不修改外部会员、积分或黑名单。
    Task<OperationResult<QualificationReference>> HandleAsync(QualificationExceptionCommand command, CancellationToken cancellationToken);
}

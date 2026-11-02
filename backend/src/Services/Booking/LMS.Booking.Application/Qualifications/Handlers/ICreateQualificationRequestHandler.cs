using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Handlers;

public interface ICreateQualificationRequestHandler
{
    // 同宾客 + 物业已有未决申请时不重复创建：已升级（ESCALATED）的原样返回；仅待复核（MANAGER_APPROVAL_REQUIRED）的升级为 ESCALATED；否则新建一条 ESCALATED 申请。
    Task<OperationResult<QualificationReference>> HandleAsync(CreateQualificationRequestCommand command, CancellationToken cancellationToken);
}

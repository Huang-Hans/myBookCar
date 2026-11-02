using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Application.Directory;

namespace LMS.Booking.Application.Approvals.Handlers;

public interface IRegisterApprovalUploadHandler
{
    Task<OperationResult<FileUploadInfo>> HandleAsync(RegisterApprovalUploadCommand command, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Handlers;

// 为本审核本材料登记一次上传：文件元数据、审计和幂等记录由 Platform（文件所有者）写入，Booking 不直接写它的库。
// 登记不改变 isVerified，也不宣称传输或扫描已完成；只返回 Platform 签发的短效上传描述。
internal sealed class RegisterApprovalUploadHandler(IApprovalRepository approvals, IPlatformDirectory platform) : IRegisterApprovalUploadHandler
{
    public async Task<OperationResult<FileUploadInfo>> HandleAsync(RegisterApprovalUploadCommand command, CancellationToken cancellationToken)
    {
        var approval = await approvals.FindVisibleAsync(command.ApprovalId, command.Context.Scope, cancellationToken);
        if (approval is null)
        {
            return OperationResult<FileUploadInfo>.Failed(OperationOutcome.NotFound);
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            return OperationResult<FileUploadInfo>.Failed(OperationOutcome.StateConflict);
        }

        var request = new RegisterFileRequest(
            approval.ApprovalId, command.DocumentType, approval.PropertyId, command.OriginalFileName, command.ContentType, command.SizeBytes, command.Sha256);
        var registered = await platform.RegisterFileAsync(request, command.IdempotencyKey, cancellationToken);

        return ToResult(registered);
    }

    private static OperationResult<FileUploadInfo> ToResult(DirectoryResult<FileUploadInfo> registered) =>
        registered.Outcome switch
        {
            DirectoryOutcome.Success => OperationResult<FileUploadInfo>.Created(registered.Value!),
            DirectoryOutcome.Rejected => OperationResult<FileUploadInfo>.Invalid(registered.Issues),
            DirectoryOutcome.Conflict => OperationResult<FileUploadInfo>.Failed(OperationOutcome.IdempotencyKeyReused),
            DirectoryOutcome.Forbidden => OperationResult<FileUploadInfo>.Failed(OperationOutcome.Forbidden),
            DirectoryOutcome.NotFound => OperationResult<FileUploadInfo>.Failed(OperationOutcome.NotFound),
            _ => OperationResult<FileUploadInfo>.Failed(OperationOutcome.SourceUnavailable)
        };
}

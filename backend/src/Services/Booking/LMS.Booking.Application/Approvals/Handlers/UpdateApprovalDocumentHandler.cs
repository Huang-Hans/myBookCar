using LMS.BuildingBlocks.Core.Operations;
using LMS.Booking.Application.Approvals.Commands;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Application.Approvals.Handlers;

// 绑定材料文件、变更人工核验结果。上传与人工核验互不替代：核验为通过时，必须有已绑定的、属于本审核本材料且安全状态为 CLEAN 的文件。
// 文件状态读取自 Platform，不写入 file_metadata.scanStatus。
internal sealed class UpdateApprovalDocumentHandler(
    PatchWorkflow workflow,
    IApprovalRepository approvals,
    IPlatformDirectory platform) : IUpdateApprovalDocumentHandler
{
    private const string Operation = "approval.document-update";
    private const string ResourceType = "cross-border-approval";

    public async Task<OperationResult<CrossBorderApproval>> HandleAsync(UpdateApprovalDocumentCommand command, CancellationToken cancellationToken)
    {
        var approval = await approvals.FindVisibleAsync(command.ApprovalId, command.Context.Scope, cancellationToken);
        if (approval is null)
        {
            return OperationResult<CrossBorderApproval>.Failed(OperationOutcome.NotFound);
        }

        var rejection = await CheckFileAsync(approval, command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private async Task<OperationResult<CrossBorderApproval>?> CheckFileAsync(
        CrossBorderApproval approval,
        UpdateApprovalDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var isVerifying = command.Changes.IsVerified is { IsSet: true, Value: true };
        if (!command.Changes.FileId.IsSet && !isVerifying)
        {
            return null;
        }

        var fileId = command.Changes.FileId.IsSet ? command.Changes.FileId.Value : approval.SlotOf(command.DocumentType).FileId;
        if (fileId is null)
        {
            return OperationResult<CrossBorderApproval>.Rejected([new FieldIssue("isVerified", "A file must be bound before the document can be verified")]);
        }

        var file = await platform.GetFileAsync(fileId, cancellationToken);

        return file.IsSuccess ? CheckBelongsAndSafe(file.Value!, approval, command, isVerifying) : FromDirectory(file);
    }

    private static OperationResult<CrossBorderApproval>? CheckBelongsAndSafe(
        RegisteredFile file,
        CrossBorderApproval approval,
        UpdateApprovalDocumentCommand command,
        bool isVerifying)
    {
        if (file.ApprovalId != approval.ApprovalId || file.DocumentType != command.DocumentType)
        {
            return OperationResult<CrossBorderApproval>.Rejected([new FieldIssue("fileId", "The file does not belong to this approval document")]);
        }

        return isVerifying && file.ScanStatus != FileScanStatus.Clean
            ? OperationResult<CrossBorderApproval>.Rejected([new FieldIssue("isVerified", "Requires a file with a clean scan status")])
            : null;
    }

    private static OperationResult<CrossBorderApproval> FromDirectory(DirectoryResult<RegisteredFile> file) =>
        file.Outcome == DirectoryOutcome.NotFound
            ? OperationResult<CrossBorderApproval>.Rejected([new FieldIssue("fileId", "The file is unknown")])
            : OperationResult<CrossBorderApproval>.Failed(OperationOutcome.SourceUnavailable);

    private PatchSpecification<CrossBorderApproval> BuildSpecification(UpdateApprovalDocumentCommand command) =>
        new(
            command.ApprovalId,
            command.ExpectedVersion,
            (id, context, token) => approvals.FindVisibleAsync(id, command.Context.Scope, context, token),
            approval => approval.Version,
            (approval, _, _) => Task.FromResult(Apply(approval, command)),
            (updated, expected, context, token) => approvals.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.ApprovalId, now, command.Changes.ChangedFields(), after.PropertyId));

    private static OperationResult<CrossBorderApproval> Apply(CrossBorderApproval current, UpdateApprovalDocumentCommand command) =>
        current.Status != ApprovalStatus.Pending
            ? OperationResult<CrossBorderApproval>.Failed(OperationOutcome.StateConflict)
            : OperationResult<CrossBorderApproval>.Succeeded(current.ApplyDocument(command.DocumentType, command.Changes));
}

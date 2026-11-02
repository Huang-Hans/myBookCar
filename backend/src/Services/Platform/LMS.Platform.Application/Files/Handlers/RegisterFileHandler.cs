using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using Microsoft.Extensions.Options;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Application.Files.Models;
using LMS.Platform.Application.Files.Options;
using LMS.Platform.Application.Files.Ports;
using LMS.Platform.Application.Files.Services;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Application.Files.Handlers;

// 登记审核材料文件：写入元数据（状态 QUARANTINED）、审计和幂等记录，并签发一次短效上传描述。
// 文件类型与大小上限来自部署配置；配置缺失时明确返回“来源不可用”。
internal sealed class RegisterFileHandler(
    CreateWorkflow workflow,
    IFileMetadataRepository files,
    UploadUrlSigner signer,
    IOptions<FileStorageOptions> options,
    TimeProvider timeProvider) : IRegisterFileHandler
{
    private const string Operation = "file.register";
    private const string ResourceType = "file";

    public async Task<OperationResult<FileRegistration>> HandleAsync(RegisterFileCommand command, CancellationToken cancellationToken)
    {
        var rejection = Precheck(command);
        if (rejection is not null)
        {
            return rejection;
        }

        var created = await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);

        return created.IsSuccess
            ? ToRegistration(created)
            : OperationResult<FileRegistration>.Failed(created.Outcome);
    }

    private OperationResult<FileRegistration>? Precheck(RegisterFileCommand command)
    {
        if (!options.Value.IsConfigured)
        {
            return OperationResult<FileRegistration>.Failed(OperationOutcome.SourceUnavailable);
        }

        if (!command.Context.Scope.Allows(command.PropertyId))
        {
            return OperationResult<FileRegistration>.Failed(OperationOutcome.Forbidden);
        }

        var issues = ValidateLimits(command);
        return issues.Count > 0 ? OperationResult<FileRegistration>.Invalid(issues) : null;
    }

    private List<FieldIssue> ValidateLimits(RegisterFileCommand command)
    {
        var issues = new List<FieldIssue>();
        var settings = options.Value;
        if (!settings.AllowedContentTypes.Contains(command.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(new FieldIssue("contentType", "Is not an allowed content type"));
        }

        if (command.SizeBytes > settings.MaxSizeBytes)
        {
            issues.Add(new FieldIssue("sizeBytes", $"Must not exceed {settings.MaxSizeBytes} bytes"));
        }

        return issues;
    }

    private CreateSpecification<FileMetadata> BuildSpecification(RegisterFileCommand command) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(FileMetadata.ForApprovalDocument(
                command.ApprovalId, command.DocumentType, command.PropertyId, command.OriginalFileName, command.ContentType,
                command.SizeBytes, command.Sha256, command.Context.Actor, timeProvider.GetUtcNow())),
            file => file.FileId,
            (file, context, token) => files.InsertAsync(file, context, token),
            (id, token) => files.FindAsync(id, token),
            (file, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, file.FileId, now, file.PropertyId));

    // 重放同样签发新的上传描述：签名无状态，旧描述过期后重试仍能继续上传。
    private OperationResult<FileRegistration> ToRegistration(OperationResult<FileMetadata> created)
    {
        var registration = new FileRegistration(created.Value!, signer.Create(created.Value!.FileId, created.Value.ContentType));

        return created.Outcome == OperationOutcome.Replayed
            ? OperationResult<FileRegistration>.Replayed(registration)
            : OperationResult<FileRegistration>.Created(registration);
    }
}

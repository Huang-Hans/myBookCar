using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Exports.Commands;
using LMS.Platform.Application.Exports.Ports;
using LMS.Platform.Application.Files.Options;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.Exports;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Exports.Handlers;

// 受理审计导出：只登记 QUEUED 的作业，真实文件由后台作业生成。结果文件的存储未配置时不受理，
// 因为那样的作业永远不可能完成。
internal sealed class CreateAuditExportHandler(
    CreateWorkflow workflow,
    IAuditExportRepository exports,
    IOptions<FileStorageOptions> storageOptions,
    TimeProvider timeProvider) : ICreateAuditExportHandler
{
    private const string Operation = "audit-export.create";
    private const string ResourceType = "audit-export";

    public async Task<OperationResult<AuditExport>> HandleAsync(CreateAuditExportCommand command, CancellationToken cancellationToken)
    {
        if (!storageOptions.Value.IsConfigured)
        {
            return OperationResult<AuditExport>.Failed(OperationOutcome.SourceUnavailable);
        }

        var rejection = Check(command);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private static OperationResult<AuditExport>? Check(CreateAuditExportCommand command)
    {
        var filter = command.Filter;
        if (filter.PropertyId is not null && !command.Context.Scope.Allows(filter.PropertyId))
        {
            return OperationResult<AuditExport>.Failed(OperationOutcome.Forbidden);
        }

        return filter.From is not null && filter.To is not null && filter.From >= filter.To
            ? OperationResult<AuditExport>.Invalid([new FieldIssue("filter.to", "Must be later than from")])
            : null;
    }

    private CreateSpecification<AuditExport> BuildSpecification(CreateAuditExportCommand command) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(AuditExport.Create(command.Filter, command.Context.Actor, command.Context.Scope, timeProvider.GetUtcNow())),
            export => export.ExportId,
            (export, context, token) => exports.InsertAsync(export, context, token),
            (id, token) => exports.FindAsync(id, token),
            (export, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, export.ExportId, now, command.Filter.PropertyId));
}

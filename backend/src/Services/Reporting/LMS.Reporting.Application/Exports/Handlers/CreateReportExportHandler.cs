using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Auditing.Services;
using LMS.Reporting.Application.Exports.Commands;
using LMS.Reporting.Application.Exports.Ports;
using LMS.Reporting.Application.Files.Options;
using LMS.Reporting.Application.Reports.Options;
using LMS.Reporting.Application.Workflows;
using LMS.Reporting.Domain.Exports;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Application.Exports.Handlers;

// 受理报表导出：只登记 QUEUED 的作业，真实文件由后台作业生成。报表来源未接入或结果存储未配置时不受理，
// 因为那样的作业不可能产出真实结果；PDF 尚未实现，明确拒绝而不是悄悄生成别的格式。
internal sealed class CreateReportExportHandler(
    CreateWorkflow workflow,
    IReportExportRepository exports,
    IOptions<ReportingOptions> reportingOptions,
    IOptions<FileStorageOptions> storageOptions,
    TimeProvider timeProvider) : ICreateReportExportHandler
{
    private const string Operation = "report-export.create";
    private const string ResourceType = "report-export";

    public async Task<OperationResult<ReportExport>> HandleAsync(CreateReportExportCommand command, CancellationToken cancellationToken)
    {
        var rejection = Check(command);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private OperationResult<ReportExport>? Check(CreateReportExportCommand command)
    {
        if (command.Format != ExportFormat.Xlsx)
        {
            return OperationResult<ReportExport>.Invalid([new FieldIssue("format", "Only XLSX is supported; PDF export is not available yet")]);
        }

        if (command.Filter.PropertyId is not null && !command.Context.Scope.Allows(command.Filter.PropertyId))
        {
            return OperationResult<ReportExport>.Failed(OperationOutcome.Forbidden);
        }

        if (command.Filter.From is not null && command.Filter.To is not null && command.Filter.From >= command.Filter.To)
        {
            return OperationResult<ReportExport>.Invalid([new FieldIssue("filter.to", "Must be later than from")]);
        }

        return reportingOptions.Value.RowsSourceConnected && storageOptions.Value.IsConfigured
            ? null
            : OperationResult<ReportExport>.Failed(OperationOutcome.SourceUnavailable);
    }

    private CreateSpecification<ReportExport> BuildSpecification(CreateReportExportCommand command) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(ReportExport.Create(
                command.ReportType, command.Format, command.Filter, command.Context.Actor, command.Context.Scope, timeProvider.GetUtcNow())),
            export => export.ExportId,
            (export, context, token) => exports.InsertAsync(export, context, token),
            (id, token) => exports.FindAsync(id, token),
            (export, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, export.ExportId, now, command.Filter.PropertyId));
}

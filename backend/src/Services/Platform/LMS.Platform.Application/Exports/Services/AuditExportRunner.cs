using System.Globalization;
using System.Runtime.CompilerServices;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.AuditRecords.Models;
using LMS.Platform.Application.AuditRecords.Ports;
using LMS.Platform.Application.Exports.Options;
using LMS.Platform.Application.Exports.Ports;
using LMS.Platform.Application.Files.Models;
using LMS.Platform.Application.Files.Ports;
using LMS.Platform.Domain.Exports;
using LMS.Platform.Domain.Files;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Exports.Services;

// 执行一个审计导出作业：领取（带租约）→ 按快照范围分批读取审计记录 → 流式写出 xlsx 到临时文件 →
// 写入文件存储 → 在同一事务内登记结果文件并把作业置为 SUCCEEDED。
// 内容取申请时刻之前的记录并按时间升序分页，导出过程中新增的审计不会造成漏行或重复。
internal sealed class AuditExportRunner(
    IAuditExportRepository exports,
    IAuditRecordReader audits,
    IWorkbookWriter workbookWriter,
    IFileContentStore store,
    IFileMetadataRepository files,
    ITransactionRunner transactions,
    IOptions<ExportOptions> options,
    TimeProvider timeProvider) : IAuditExportRunner
{
    private const string WorkbookContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string FailureMessage = "The export could not be generated.";
    private const string TooLargeMessage = "The export exceeds the maximum number of rows or the maximum file size.";
    private const int SpoolBufferSize = 81920;

    private static readonly IReadOnlyList<SortTerm> ChronologicalOrder = [new("occurredAt", false), new("auditId", false)];

    public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var export = await exports.ClaimNextAsync(now, now.AddMinutes(options.Value.LeaseMinutes), cancellationToken);
        if (export is null)
        {
            return false;
        }

        await ExecuteAsync(export, cancellationToken);

        return true;
    }

    // 取消（服务停止）不改变作业状态，租约过期后会被重新领取；其他失败标记为 FAILED 后继续向上抛出供记录日志。
    private async Task ExecuteAsync(AuditExport export, CancellationToken cancellationToken)
    {
        try
        {
            await GenerateAsync(export, cancellationToken);
        }
        catch (ExportTooLargeException)
        {
            await exports.FailAsync(export.ExportId, TooLargeMessage, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await exports.FailAsync(export.ExportId, FailureMessage, CancellationToken.None);
            throw;
        }
    }

    private async Task GenerateAsync(AuditExport export, CancellationToken cancellationToken)
    {
        await using var spool = new FileStream(
            Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, SpoolBufferSize,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await workbookWriter.WriteAsync(
            spool, AuditExportSheet.SheetName, AuditExportSheet.Headers, ReadRowsAsync(export, cancellationToken), cancellationToken);
        spool.Position = 0;

        await StoreAndCompleteAsync(export, spool, cancellationToken);
    }

    private async IAsyncEnumerable<IReadOnlyList<string>> ReadRowsAsync(
        AuditExport export,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var batchSize = options.Value.BatchSize;
        for (var page = 1; ; page++)
        {
            var result = await audits.ListAsync(BuildQuery(export, page, batchSize), cancellationToken);
            if (result.TotalItems > options.Value.MaxRows)
            {
                throw new ExportTooLargeException();
            }

            foreach (var record in result.Items)
            {
                yield return AuditExportSheet.RowOf(record);
            }

            if (result.Items.Count == 0 || (long)page * batchSize >= result.TotalItems)
            {
                yield break;
            }
        }
    }

    private static ListAuditRecordsQuery BuildQuery(AuditExport export, int page, int batchSize)
    {
        var filter = export.Filter;
        var upperBound = filter.To is { } to && to < export.RequestedAt ? to : export.RequestedAt;
        var list = new ListQuery(page, batchSize, filter.Search ?? string.Empty, ChronologicalOrder);

        return new ListAuditRecordsQuery(list, export.Scope, filter.PropertyId, filter.From, upperBound);
    }

    private async Task StoreAndCompleteAsync(AuditExport export, Stream content, CancellationToken cancellationToken)
    {
        var token = Guid.CreateVersion7().ToString("N");
        var stored = await store.WriteAsync(token, content, options.Value.MaxFileBytes, cancellationToken);
        if (stored.Outcome == StoreOutcome.Exceeded)
        {
            throw new ExportTooLargeException();
        }

        if (stored.Outcome != StoreOutcome.Stored)
        {
            throw new InvalidOperationException("The export result could not be stored.");
        }

        var now = timeProvider.GetUtcNow();
        var file = FileMetadata.ForExportResult(
            token, export.ExportId, FileNameOf(export), WorkbookContentType, stored.Length, stored.Sha256, export.RequestedBy, now);
        var completed = await CompleteAsync(export, file, now.AddHours(options.Value.ResultRetentionHours), cancellationToken);
        if (!completed)
        {
            await store.DeleteAsync(token, CancellationToken.None);
        }
    }

    // 作业若已被其他执行者接手（租约过期后重新领取）则不再覆盖，返回 false 由调用方清理本次写入的文件。
    private Task<bool> CompleteAsync(AuditExport export, FileMetadata file, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        transactions.ExecuteAsync(
            async (context, token) =>
            {
                if (!await exports.CompleteAsync(export.ExportId, file.FileId, expiresAt, context, token))
                {
                    return false;
                }

                await files.InsertAsync(file, context, token);

                return true;
            },
            cancellationToken);

    private static string FileNameOf(AuditExport export) =>
        $"audit-log-{export.RequestedAt.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}.xlsx";
}

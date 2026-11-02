using System.Globalization;
using System.Runtime.CompilerServices;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Exports.Options;
using LMS.Reporting.Application.Exports.Ports;
using LMS.Reporting.Application.Files.Models;
using LMS.Reporting.Application.Files.Ports;
using LMS.Reporting.Application.Reports.Models;
using LMS.Reporting.Application.Reports.Options;
using LMS.Reporting.Application.Reports.Ports;
using LMS.Reporting.Domain.Exports;
using LMS.Reporting.Domain.Files;
using LMS.Reporting.Domain.Rows;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Application.Exports.Services;

// 执行一个报表导出作业：领取（带租约）→ 复用报表查询的同一读取口径与申请时的授权快照分批读取行 →
// 流式写出 xlsx 到临时文件 → 写入文件存储 → 在同一事务内登记结果文件并把作业置为 SUCCEEDED。
// 报表来源未接入时作业直接按失败结束，不生成空文件冒充报表。
internal sealed class ReportExportRunner(
    IReportExportRepository exports,
    IReportRowReader rows,
    IWorkbookWriter workbookWriter,
    IFileContentStore store,
    IFileMetadataRepository files,
    ITransactionRunner transactions,
    IOptions<ExportOptions> options,
    IOptions<ReportingOptions> reportingOptions,
    TimeProvider timeProvider) : IReportExportRunner
{
    private const string WorkbookContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string FailureMessage = "The export could not be generated.";
    private const string TooLargeMessage = "The export exceeds the maximum number of rows or the maximum file size.";
    private const string SourceUnavailableMessage = "The report data source is not available.";
    private const int SpoolBufferSize = 81920;

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
    private async Task ExecuteAsync(ReportExport export, CancellationToken cancellationToken)
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

    private async Task GenerateAsync(ReportExport export, CancellationToken cancellationToken)
    {
        if (!reportingOptions.Value.RowsSourceConnected)
        {
            await exports.FailAsync(export.ExportId, SourceUnavailableMessage, CancellationToken.None);

            return;
        }

        await using var spool = new FileStream(
            Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, SpoolBufferSize,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await workbookWriter.WriteAsync(
            spool,
            ReportExportSheet.SheetNameOf(export.ReportType),
            ReportExportSheet.HeadersOf(export.ReportType),
            ReadRowsAsync(export, cancellationToken),
            cancellationToken);
        spool.Position = 0;

        await StoreAndCompleteAsync(export, spool, cancellationToken);
    }

    private async IAsyncEnumerable<IReadOnlyList<string>> ReadRowsAsync(
        ReportExport export,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var batchSize = options.Value.BatchSize;
        for (var page = 1; ; page++)
        {
            var result = await rows.ListAsync(BuildQuery(export, page, batchSize), cancellationToken);
            if (result.TotalItems > options.Value.MaxRows)
            {
                throw new ExportTooLargeException();
            }

            foreach (var row in result.Items)
            {
                yield return ReportExportSheet.RowOf(row);
            }

            if (result.Items.Count == 0 || (long)page * batchSize >= result.TotalItems)
            {
                yield break;
            }
        }
    }

    private static ReportRowsQuery BuildQuery(ReportExport export, int page, int batchSize)
    {
        var filter = export.Filter;
        var list = new ListQuery(page, batchSize, string.Empty, SortTermsOf(ReportSorting.Of(export.ReportType).DefaultSort));

        return new ReportRowsQuery(export.ReportType, list, export.Scope, filter.PropertyId, filter.From, filter.To, filter.ServiceType);
    }

    private static List<SortTerm> SortTermsOf(string sort) => sort
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(term => term.StartsWith('-') ? new SortTerm(term[1..], true) : new SortTerm(term, false))
        .ToList();

    private async Task StoreAndCompleteAsync(ReportExport export, Stream content, CancellationToken cancellationToken)
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
    private Task<bool> CompleteAsync(ReportExport export, FileMetadata file, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
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

    private static string FileNameOf(ReportExport export) =>
        $"{ReportSlug(export.ReportType)}-{export.RequestedAt.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}.xlsx";

    private static string ReportSlug(ReportType type) => type switch
    {
        ReportType.DailyJobSummary => "daily-job-summary",
        ReportType.TripDetail => "trip-details",
        ReportType.DriverUtilization => "driver-utilization",
        ReportType.VehicleUtilization => "vehicle-utilization",
        ReportType.Cancellation => "cancellations",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown report type.")
    };
}

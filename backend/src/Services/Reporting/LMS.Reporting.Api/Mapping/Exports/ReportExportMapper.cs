using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Reporting.Api.Contracts.Exports;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Exports.Commands;
using LMS.Reporting.Application.Exports.Models;
using LMS.Reporting.Domain.Exports;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Mapping.Exports;

internal static class ReportExportMapper
{
    private const string ReportKind = "REPORT";

    public static RequestParseResult<CreateReportExportCommand> ToCommand(
        CreateReportExportRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateReportExportCommand(
            errors.RequiredEnum<ReportType>("reportType", request.ReportType),
            errors.RequiredEnum<ExportFormat>("format", request.Format),
            ReadFilter(errors, request.Filter),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateReportExportCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateReportExportCommand>.Success(command);
    }

    public static ExportOperationView ToView(ReportExport export) => new(
        export.ExportId,
        ReportKind,
        WireEnum.ToWire(export.ReportType),
        WireEnum.ToWire(export.Format),
        export.RequestedAt.UtcDateTime,
        WireEnum.ToWire(export.State),
        export.ResultFileId,
        export.FailureMessage,
        export.ExpiresAt?.UtcDateTime);

    public static DownloadDescriptionView ToView(ExportDownload download) =>
        new(download.Url, download.FileName, download.ContentType, download.ExpiresAt.UtcDateTime);

    private static ReportExportFilter ReadFilter(FieldErrorCollector errors, ReportFilterRequest? value)
    {
        if (value is null)
        {
            errors.Add("filter", "Is required");
            return new ReportExportFilter(null, null, null, null);
        }

        return new ReportExportFilter(
            value.PropertyId is null ? null : errors.RequiredText("filter.propertyId", value.PropertyId),
            errors.OptionalUtcInstant("filter.from", value.From),
            errors.OptionalUtcInstant("filter.to", value.To),
            ReadServiceType(errors, value.ServiceType));
    }

    private static ServiceType? ReadServiceType(FieldErrorCollector errors, string? value) =>
        value is null ? null : errors.RequiredEnum<ServiceType>("filter.serviceType", value);
}

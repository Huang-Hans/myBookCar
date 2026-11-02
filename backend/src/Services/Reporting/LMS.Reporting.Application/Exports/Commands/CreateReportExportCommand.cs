using System.Globalization;
using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Domain.Exports;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Exports.Commands;

public sealed record CreateReportExportCommand(
    ReportType ReportType,
    ExportFormat Format,
    ReportExportFilter Filter,
    string IdempotencyKey,
    CommandContext Context)
{
    // 幂等指纹按导出内容计算；时间统一成 UTC 的往返格式。
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            WireEnum.ToWire(ReportType),
            WireEnum.ToWire(Format),
            Filter.PropertyId,
            Filter.From?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            Filter.To?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            Filter.ServiceType is null ? null : WireEnum.ToWire(Filter.ServiceType.Value)
        });
}

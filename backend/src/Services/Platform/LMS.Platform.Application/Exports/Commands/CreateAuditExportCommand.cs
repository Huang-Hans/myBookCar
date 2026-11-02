using System.Globalization;
using System.Text.Json;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Exports;

namespace LMS.Platform.Application.Exports.Commands;

public sealed record CreateAuditExportCommand(AuditExportFilter Filter, string IdempotencyKey, CommandContext Context)
{
    // 幂等指纹按筛选内容计算；时间统一成 UTC 的往返格式。
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Filter.PropertyId,
            Filter.From?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            Filter.To?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            Filter.Search
        });
}

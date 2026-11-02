using System.Globalization;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Domain.Building;

// 重算一个业务日所需的全部来源与本次运行的版本信息。所有行的统计窗口都是该业务日 [StartOf(Day), EndOf(Day))。
public sealed record ReportBuildContext(
    DateOnly Day,
    IReadOnlyList<ReportTripFact> Trips,
    IReadOnlyList<ReportShiftFact> Shifts,
    IReadOnlyDictionary<string, CancellationReasonInfo> Reasons,
    IReadOnlySet<string> NoShowReasonCodes,
    long SourceVersion,
    DateTimeOffset UpdatedAt)
{
    public string DayKey => Day.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    public ReportRow Row(string rowId, ReportType type, string propertyId, ServiceType? serviceType, ReportPayload payload) =>
        new(rowId, type, new ReportRowScope(propertyId, BusinessDay.StartOf(Day), BusinessDay.EndOf(Day), serviceType), payload, SourceVersion, UpdatedAt);
}

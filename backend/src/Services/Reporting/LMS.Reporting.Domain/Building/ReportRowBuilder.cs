using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Domain.Building;

// 由一个业务日的来源算出五类报表的全部行。纯计算，不读写存储；同样的来源总是得到同样的行与行标识。
public static class ReportRowBuilder
{
    public static IReadOnlyList<ReportRow> Build(ReportBuildContext context) =>
    [
        .. DailyJobSummaryRows.Build(context),
        .. TripDetailRows.Build(context),
        .. DriverUtilizationRows.Build(context),
        .. VehicleUtilizationRows.Build(context),
        .. CancellationRows.Build(context)
    ];
}

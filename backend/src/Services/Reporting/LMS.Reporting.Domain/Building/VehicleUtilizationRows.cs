using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Domain.Building;

// 车辆利用率：每个“车辆文本 × 物业 × 服务类型”每天一行。行程上没有车辆标识，只能按指派时的车辆文本分组。
// 里程是各单出发到完成的轨迹距离之和：全部单都没有轨迹时为 null，只有部分单有轨迹时标记为部分数据。停用时长与权益引用没有来源，为 null。
public static class VehicleUtilizationRows
{
    public static IEnumerable<ReportRow> Build(ReportBuildContext context) =>
        context.Trips
            .Where(trip => trip.IsCompleted && !string.IsNullOrWhiteSpace(trip.VehicleLabel))
            .GroupBy(trip => (Label: trip.VehicleLabel!.Trim(), trip.PropertyId, trip.ServiceType))
            .Select(group => context.Row(
                $"vehicle:{context.DayKey}:{group.Key.PropertyId}:{group.Key.ServiceType}:{group.Key.Label}",
                ReportType.VehicleUtilization,
                group.Key.PropertyId,
                group.Key.ServiceType,
                PayloadOf(group.Key.Label, group.ToList())));

    private static VehicleUtilizationPayload PayloadOf(string label, IReadOnlyList<ReportTripFact> trips)
    {
        var known = trips.Where(trip => trip.EnRouteToCompleteMeters is not null).Select(trip => trip.EnRouteToCompleteMeters!.Value).ToList();
        double? mileage = known.Count == 0 ? null : known.Sum();

        return new VehicleUtilizationPayload(label, mileage, trips.Count, null, null, known.Count > 0 && known.Count < trips.Count);
    }
}

using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Sources;

namespace LMS.Reporting.Domain.Building;

// 司机利用率：每个司机每天一行总计（不区分服务类型，含空闲与加班），另按服务类型各一行，只含该服务的完成行程数。
// 当天有当值班次但没有完成行程的司机也出一行总计，空闲即整段班次。没有当值班次时空闲与加班未知，为 null。
// 物业取当天当值班次的物业；没有班次时取第一单的物业。
public static class DriverUtilizationRows
{
    public static IEnumerable<ReportRow> Build(ReportBuildContext context)
    {
        var tripsByDriver = context.Trips
            .Where(trip => trip.IsCompleted && trip.DriverId is not null)
            .ToLookup(trip => trip.DriverId!);
        var shiftsByDriver = context.Shifts
            .Where(shift => shift.IsOnDuty && shift.BusinessDate == context.Day)
            .ToLookup(shift => shift.DriverId);
        var driverIds = tripsByDriver.Select(group => group.Key).Union(shiftsByDriver.Select(group => group.Key)).Order(StringComparer.Ordinal);

        return driverIds.SelectMany(driverId => RowsOf(context, driverId, tripsByDriver[driverId].ToList(), shiftsByDriver[driverId].ToList()));
    }

    private static IEnumerable<ReportRow> RowsOf(
        ReportBuildContext context,
        string driverId,
        IReadOnlyList<ReportTripFact> trips,
        IReadOnlyList<ReportShiftFact> shifts)
    {
        var name = trips.OrderBy(trip => trip.CompletedAt).LastOrDefault()?.DriverName ?? shifts.FirstOrDefault()?.DriverName ?? driverId;
        var propertyId = shifts.FirstOrDefault()?.PropertyId ?? trips[0].PropertyId;
        var workTime = shifts.Count == 0 ? null : WorkTimeOf(context.Day, trips, shifts);
        var total = new DriverUtilizationPayload(
            driverId, name, propertyId, trips.Count, Seconds(workTime?.Idle), Seconds(workTime?.Overtime), null);

        return trips
            .GroupBy(trip => trip.ServiceType)
            .Select(group => context.Row(
                $"driver:{context.DayKey}:{driverId}:{group.Key}",
                ReportType.DriverUtilization,
                propertyId,
                group.Key,
                new DriverUtilizationPayload(driverId, name, propertyId, group.Count(), null, null, null)))
            .Prepend(context.Row($"driver:{context.DayKey}:{driverId}", ReportType.DriverUtilization, propertyId, null, total));
    }

    private static DriverWorkTime WorkTimeOf(DateOnly day, IReadOnlyList<ReportTripFact> trips, IReadOnlyList<ReportShiftFact> shifts) =>
        DriverWorkTime.Of(
            shifts.Select(shift => BusinessDay.ShiftWindow(day, shift.Period)),
            trips.Where(trip => trip.EnRouteAt is not null && trip.CompletedAt is not null).Select(trip => new TimeInterval(trip.EnRouteAt!.Value, trip.CompletedAt!.Value)));

    private static long? Seconds(TimeSpan? value) => value is null ? null : (long)value.Value.TotalSeconds;
}

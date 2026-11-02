using LMS.Reporting.Api.Contracts.Reports;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Mapping.Reports;

// 每个视图只读取自己 rowType 对应的载荷；行与路由报表类型不一致说明数据损坏，按内部错误处理而不是悄悄丢行。
internal static class ReportViewMapper
{
    public static DailyJobSummaryRowView ToDailyJobSummary(ReportRow row)
    {
        var payload = PayloadOf<DailyJobSummaryPayload>(row);

        return new DailyJobSummaryRowView(
            row.RowId, payload.PropertyId, payload.Bookings, payload.Completed, payload.Cancellations, payload.NoShows,
            payload.RedemptionSummary, row.UpdatedAt.UtcDateTime);
    }

    public static TripDetailRowView ToTripDetail(ReportRow row)
    {
        var payload = PayloadOf<TripDetailPayload>(row);
        var loyalty = new LoyaltyReferenceView(
            payload.Loyalty.RedemptionId, payload.Loyalty.PointsRequired, payload.Loyalty.Entitlement, payload.Loyalty.RedemptionState);

        return new TripDetailRowView(
            row.RowId, payload.TripReference, payload.PropertyId, payload.GuestName, payload.ScheduledAt.UtcDateTime,
            payload.ActualAt?.UtcDateTime, payload.DriverId, payload.DriverName, payload.VehicleDescription, payload.DistanceMeters,
            payload.DurationSeconds, loyalty, payload.Source, row.UpdatedAt.UtcDateTime);
    }

    public static DriverUtilizationRowView ToDriverUtilization(ReportRow row)
    {
        var payload = PayloadOf<DriverUtilizationPayload>(row);

        return new DriverUtilizationRowView(
            row.RowId, payload.DriverId, payload.DriverName, payload.PropertyId, payload.TripCount, payload.IdleSeconds,
            payload.OvertimeSeconds, payload.RedemptionContributionReference, payload.IsPartial, row.UpdatedAt.UtcDateTime);
    }

    public static VehicleUtilizationRowView ToVehicleUtilization(ReportRow row)
    {
        var payload = PayloadOf<VehicleUtilizationPayload>(row);

        return new VehicleUtilizationRowView(
            row.RowId, payload.VehicleDescription, payload.MileageMeters, payload.TripCount, payload.DowntimeSeconds,
            payload.RedemptionReference, payload.IsPartial, row.UpdatedAt.UtcDateTime);
    }

    public static CancellationReportRowView ToCancellation(ReportRow row)
    {
        var payload = PayloadOf<CancellationPayload>(row);

        return new CancellationReportRowView(
            row.RowId, payload.ReasonCode, payload.Reason, payload.Source, payload.NoticeSecondsBeforePickup,
            payload.PointsReturnPolicyCode, payload.PointsReturnReference, row.UpdatedAt.UtcDateTime);
    }

    private static TPayload PayloadOf<TPayload>(ReportRow row) where TPayload : ReportPayload =>
        row.Payload as TPayload ?? throw new InvalidOperationException($"The report row '{row.RowId}' does not carry a {typeof(TPayload).Name}.");
}

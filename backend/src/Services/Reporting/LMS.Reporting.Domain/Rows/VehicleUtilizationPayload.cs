namespace LMS.Reporting.Domain.Rows;

public sealed record VehicleUtilizationPayload(
    string VehicleDescription,
    double? MileageMeters,
    int TripCount,
    long? DowntimeSeconds,
    string? RedemptionReference,
    bool IsPartial = false) : ReportPayload;

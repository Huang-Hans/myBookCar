namespace LMS.Reporting.Domain.Rows;

public sealed record DriverUtilizationPayload(
    string DriverId,
    string DriverName,
    string PropertyId,
    int TripCount,
    long? IdleSeconds,
    long? OvertimeSeconds,
    string? RedemptionContributionReference,
    bool IsPartial = false) : ReportPayload;

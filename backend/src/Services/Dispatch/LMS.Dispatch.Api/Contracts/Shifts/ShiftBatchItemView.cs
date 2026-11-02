namespace LMS.Dispatch.Api.Contracts.Shifts;

/// <summary>Existing inaccessible shifts expose only the selected driver, date and isExisting flag.</summary>
public sealed record ShiftBatchItemView(string BusinessDate, string DriverId, string DriverName, bool IsExisting,
    string? Period, string? Status, string? Role, string? ShiftId, string? PropertyId);

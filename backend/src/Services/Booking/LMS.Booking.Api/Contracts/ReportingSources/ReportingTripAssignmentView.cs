namespace LMS.Booking.Api.Contracts.ReportingSources;

/// <summary>
/// 指派时的司机与车辆快照。
/// </summary>
/// <param name="DriverId">司机标识。</param>
/// <param name="DriverName">司机姓名快照。</param>
/// <param name="VehicleLabel">车辆文本快照。</param>
public sealed record ReportingTripAssignmentView(string DriverId, string DriverName, string VehicleLabel);

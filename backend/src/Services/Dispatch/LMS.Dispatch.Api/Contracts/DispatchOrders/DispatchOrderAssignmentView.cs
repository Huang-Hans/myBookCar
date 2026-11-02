namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>Booking 的权威司机指派；司机姓名与车辆文本是指派时的快照。</summary>
/// <param name="OfferId">促成指派的邀请标识。</param>
/// <param name="DriverId">司机标识。</param>
/// <param name="DriverName">指派时的司机姓名。</param>
/// <param name="VehicleLabel">指派时的车辆文本。</param>
/// <param name="AssignedAt">指派时间（UTC）。</param>
public sealed record DispatchOrderAssignmentView(string OfferId, string DriverId, string DriverName, string VehicleLabel, DateTimeOffset AssignedAt);

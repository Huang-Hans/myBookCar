namespace LMS.Booking.Api.Contracts.DispatchOrders;

/// <summary>权威司机指派快照：司机姓名与车辆文本是指派时的值。</summary>
public sealed record DispatchOrderAssignmentView(
    string OfferId,
    string DriverId,
    string DriverName,
    string VehicleLabel,
    DateTimeOffset AssignedAt);

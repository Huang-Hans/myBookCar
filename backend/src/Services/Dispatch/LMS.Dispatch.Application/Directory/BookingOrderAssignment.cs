namespace LMS.Dispatch.Application.Directory;

// Booking 的权威司机指派快照：司机姓名与车辆文本是指派时的值，不随司机主档后续修改而变化。
public sealed record BookingOrderAssignment(
    string OfferId,
    string DriverId,
    string DriverName,
    string VehicleLabel,
    DateTimeOffset AssignedAt);

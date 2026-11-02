namespace LMS.Booking.Domain.Trips;

// 行程的权威司机指派。OfferId 来自 Dispatch 的邀请，是指派的稳定操作标识：同一邀请重复指派得到同一结果。
// 司机姓名与车辆文本是指派时的快照，供客户与历史展示，不随司机主档后续修改而变化。
public sealed record DriverAssignment(
    string OfferId,
    string DriverId,
    string DriverName,
    string VehicleLabel,
    DateTimeOffset AssignedAt);

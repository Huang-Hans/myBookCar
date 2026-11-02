namespace LMS.Dispatch.Application.Directory;

// 向 Booking 请求条件指派。OfferId 是稳定的业务操作标识：重试同一邀请不会产生第二笔指派。
public sealed record AssignDriverRequest(
    string TripId,
    string OfferId,
    string DriverId,
    string DriverName,
    string VehicleLabel,
    string PropertyId);

namespace LMS.Booking.Api.Contracts.DriverTrips;

/// <summary>
/// 条件指派请求（服务间，来自 Dispatch）。driverId 必须等于令牌绑定的司机；offerId 是稳定的操作标识。
/// </summary>
/// <param name="OfferId">Dispatch 的邀请标识。</param>
/// <param name="DriverId">被指派的司机标识。</param>
/// <param name="DriverName">指派时的司机姓名快照。</param>
/// <param name="VehicleLabel">指派时的车辆文本快照。</param>
/// <param name="PropertyId">邀请所属物业，必须与行程物业一致。</param>
public sealed record DriverAssignmentRequest(string? OfferId, string? DriverId, string? DriverName, string? VehicleLabel, string? PropertyId);

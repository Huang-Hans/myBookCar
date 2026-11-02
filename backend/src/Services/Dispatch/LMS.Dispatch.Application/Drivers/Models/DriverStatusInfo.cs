using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Models;

// 服务间只读的司机状态：Booking 在审批前核对“司机是否 Active”和黑名单命中。不含个人信息。
public sealed record DriverStatusInfo(string DriverId, string PropertyId, DriverStatus Status, string? RestrictionReason);

namespace LMS.Booking.Application.Directory;

// Dispatch 提供的司机状态快照。RestrictionReason 是停用限制的说明（若有）。
public sealed record DriverStatusSnapshot(string DriverId, string PropertyId, DirectoryDriverStatus Status, string? RestrictionReason);

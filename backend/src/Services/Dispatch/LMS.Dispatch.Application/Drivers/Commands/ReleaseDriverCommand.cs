using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Drivers.Commands;

// Booking 通知：司机执行的行程已在 EndedAt 完成或取消。
public sealed record ReleaseDriverCommand(string DriverId, string TripId, DateTimeOffset EndedAt, CommandContext Context);

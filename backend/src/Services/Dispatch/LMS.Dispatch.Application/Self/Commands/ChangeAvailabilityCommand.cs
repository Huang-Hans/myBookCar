using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Self.Commands;

// 司机本人切换工作状态。DriverId 来自令牌；不接受期望版本，并发冲突由仓储的版本条件写入兜底。
public sealed record ChangeAvailabilityCommand(string DriverId, DriverAvailability Target, CommandContext Context);

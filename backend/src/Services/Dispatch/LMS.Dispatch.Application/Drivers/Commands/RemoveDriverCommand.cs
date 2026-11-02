using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Drivers.Commands;

public sealed record RemoveDriverCommand(string DriverId, CommandContext Context);

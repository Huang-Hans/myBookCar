using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Commands;

public sealed record UpdateDriverCommand(string DriverId, long ExpectedVersion, DriverChanges Changes, CommandContext Context);

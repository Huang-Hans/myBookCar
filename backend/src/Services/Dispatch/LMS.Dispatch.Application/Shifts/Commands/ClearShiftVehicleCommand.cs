using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Shifts.Commands;

public sealed record ClearShiftVehicleCommand(string ShiftId, long ExpectedVersion, CommandContext Context);

using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Shifts.Commands;

public sealed record AssignShiftVehicleCommand(string ShiftId, long ExpectedVersion, string VehiclePlate, CommandContext Context);

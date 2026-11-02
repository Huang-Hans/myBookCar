using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Commands;

public sealed record UpdateShiftCommand(string ShiftId, long ExpectedVersion, ShiftChanges Changes, CommandContext Context);

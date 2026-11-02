using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Models;

internal sealed record ShiftSlot(DateOnly BusinessDate, ShiftPeriod Period);

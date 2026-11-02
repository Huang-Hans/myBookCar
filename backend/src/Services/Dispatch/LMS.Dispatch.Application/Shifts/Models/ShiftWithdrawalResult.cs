using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Models;

public sealed record ShiftWithdrawalResult(DateOnly BusinessDate, ShiftPeriod Period, int WithdrawnCount);

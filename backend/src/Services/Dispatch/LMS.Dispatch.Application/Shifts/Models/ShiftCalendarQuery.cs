using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Dispatch.Application.Shifts.Models;

public sealed record ShiftCalendarQuery(DateOnly From, DateOnly To, string? PropertyId, PropertyScope Scope);

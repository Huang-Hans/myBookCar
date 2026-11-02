using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Dispatch.Application.Shifts.Models;

public sealed record ShiftBoardQuery(DateOnly? BusinessDate, string? PropertyId, PropertyScope Scope, DateTimeOffset? PickupAt = null);

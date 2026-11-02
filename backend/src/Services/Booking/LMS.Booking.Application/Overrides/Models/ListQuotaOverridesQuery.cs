using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.Overrides.Models;

public sealed record ListQuotaOverridesQuery(ListQuery List, PropertyScope Scope, string? PropertyId);

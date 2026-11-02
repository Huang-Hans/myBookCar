using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.Quotas.Models;

public sealed record ListPropertyQuotasQuery(ListQuery List, PropertyScope Scope, string? PropertyId);

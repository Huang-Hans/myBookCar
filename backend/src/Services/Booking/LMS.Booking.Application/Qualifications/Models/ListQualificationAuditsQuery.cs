using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.Qualifications.Models;

public sealed record ListQualificationAuditsQuery(ListQuery List, PropertyScope Scope, string? PropertyId);

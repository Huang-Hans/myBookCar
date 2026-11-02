using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Application.Qualifications.Models;

public sealed record ListQualificationsQuery(ListQuery List, PropertyScope Scope, string? PropertyId, QualificationStatus? Status);

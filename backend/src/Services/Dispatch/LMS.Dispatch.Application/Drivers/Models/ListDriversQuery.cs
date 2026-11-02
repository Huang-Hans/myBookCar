using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Models;

public sealed record ListDriversQuery(
    ListQuery List,
    PropertyScope Scope,
    string? PropertyId,
    DriverStatus? Status,
    DriverViewFilter View);

using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Dispatch.Application.Vehicles.Models;

public sealed record VehicleSummaryQuery(PropertyScope Scope, string? PropertyId);

using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Application.Vehicles.Models;

public sealed record ListVehiclesQuery(
    ListQuery List,
    PropertyScope Scope,
    string? PropertyId,
    VehicleGrade? Grade,
    VehicleStatus? Status);

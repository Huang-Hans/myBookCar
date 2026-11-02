using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Application.Vehicles.Models;

public sealed record VehicleCard(Vehicle Vehicle, string? DriverName);

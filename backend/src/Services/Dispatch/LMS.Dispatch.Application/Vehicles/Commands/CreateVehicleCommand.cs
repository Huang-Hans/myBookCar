using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Application.Vehicles.Commands;

public sealed record CreateVehicleCommand(
    string Plate,
    string Model,
    VehicleGrade Grade,
    string PropertyId,
    int Seats,
    int MileageKm,
    VehicleStatus Status,
    string? DriverId,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Plate.ToUpperInvariant(),
            Model,
            WireEnum.ToWire(Grade),
            PropertyId,
            Seats,
            MileageKm,
            WireEnum.ToWire(Status),
            DriverId
        });
}

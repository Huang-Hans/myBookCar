using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Commands;

public sealed record CreateLocationCommand(
    string Name,
    string Address,
    Coordinates Coordinates,
    LocationRegion Region,
    string CategoryCode,
    string ContactPerson,
    string ContactPhone,
    string OperatingHours,
    Geofence Geofence,
    bool IsActive,
    string? Notes,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Name, Address, Coordinates.Latitude, Coordinates.Longitude, WireEnum.ToWire(Region), CategoryCode, ContactPerson,
            ContactPhone, OperatingHours, Geofence.IsEnabled, Geofence.RadiusMeters, IsActive, Notes
        });
}

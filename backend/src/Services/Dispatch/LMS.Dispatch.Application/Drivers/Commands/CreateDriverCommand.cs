using System.Globalization;
using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Commands;

public sealed record CreateDriverCommand(
    string Name,
    string LicenseNumber,
    DateOnly LicenseExpiryDate,
    string Phone,
    string PropertyId,
    string? AssignedVehicleId,
    DriverStatus Status,
    IReadOnlyList<string> Languages,
    EmergencyContact EmergencyContact,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Name,
            LicenseNumber.ToLowerInvariant(),
            LicenseExpiryDate.ToString("O", CultureInfo.InvariantCulture),
            Phone,
            PropertyId,
            AssignedVehicleId,
            WireEnum.ToWire(Status),
            Languages,
            EmergencyContact.Name,
            EmergencyContact.Phone
        });
}

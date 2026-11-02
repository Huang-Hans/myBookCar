using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Domain.Drivers;

// 司机主档的局部更新。可用状态、同步来源和限制信息是只读字段，不在此列。
public sealed record DriverChanges(
    Optional<string> Name,
    Optional<string> LicenseNumber,
    Optional<DateOnly> LicenseExpiryDate,
    Optional<string> Phone,
    Optional<string> PropertyId,
    Optional<string> AssignedVehicleId,
    Optional<DriverStatus> Status,
    Optional<IReadOnlyList<string>> Languages,
    EmergencyContactChanges? EmergencyContact)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(
            ("name", Name.IsSet),
            ("licenseNumber", LicenseNumber.IsSet),
            ("licenseExpiryDate", LicenseExpiryDate.IsSet),
            ("phone", Phone.IsSet),
            ("propertyId", PropertyId.IsSet),
            ("assignedVehicleId", AssignedVehicleId.IsSet),
            ("status", Status.IsSet),
            ("languages", Languages.IsSet),
            ("emergencyContact", EmergencyContact is not null));
}

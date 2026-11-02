using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Domain.Drivers;

// 司机主档。Availability、AvailabilitySource、SyncedAt 只读，由可信同步写入，管理端不能修改。
// 同步来源尚未接入时三者为空，不默认伪造 Off Duty。
public sealed record Driver(
    string DriverId,
    string Name,
    string LicenseNumber,
    DateOnly LicenseExpiryDate,
    string Phone,
    string PropertyId,
    string AssignedVehicle,
    DriverStatus Status,
    IReadOnlyList<string> Languages,
    EmergencyContact EmergencyContact,
    DriverAvailability? Availability,
    string? AvailabilitySource,
    DateTimeOffset? SyncedAt,
    DriverRestriction? Restriction,
    long Version)
{
    public string? AssignedVehicleId { get; init; }

    // Current bound vehicle projection; never persisted on the driver.
    public VehicleGrade? AssignedVehicleGrade { get; init; }

    public DateTimeOffset? DeletedAt { get; init; }

    private const string IdPrefix = "driver_";
    private const long FirstVersion = 1;

    public static Driver Create(
        string name,
        string licenseNumber,
        DateOnly licenseExpiryDate,
        string phone,
        string propertyId,
        string assignedVehicle,
        DriverStatus status,
        IReadOnlyList<string> languages,
        EmergencyContact emergencyContact,
        DateOnly businessDate) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            name, licenseNumber, licenseExpiryDate, phone, propertyId, assignedVehicle, status, languages, emergencyContact,
            null, null, null,
            DriverRestriction.For(status, businessDate),
            FirstVersion);

    // 状态变为 INACTIVE 或 SUSPENDED 时登记限制（生效日取业务日期，原因随状态），变回 ACTIVE 时清除；状态未变保持原限制。
    public Driver Apply(DriverChanges changes, DateOnly businessDate)
    {
        var status = changes.Status.ValueOr(Status);

        return this with
        {
            Name = changes.Name.ValueOr(Name),
            LicenseNumber = changes.LicenseNumber.ValueOr(LicenseNumber),
            LicenseExpiryDate = changes.LicenseExpiryDate.ValueOr(LicenseExpiryDate),
            Phone = changes.Phone.ValueOr(Phone),
            PropertyId = changes.PropertyId.ValueOr(PropertyId),
            AssignedVehicleId = changes.AssignedVehicleId.ValueOrCurrent(AssignedVehicleId),
            AssignedVehicle = changes.AssignedVehicleId.IsSet ? string.Empty : AssignedVehicle,
            Status = status,
            Languages = changes.Languages.ValueOr(Languages),
            EmergencyContact = changes.EmergencyContact?.ApplyTo(EmergencyContact) ?? EmergencyContact,
            Restriction = ResolveRestriction(status, businessDate)
        };
    }

    // 可用状态由司机本人上报或履约事实驱动写入，同时记录来源与同步时间；规则校验见 DriverSelfRules。
    public Driver WithAvailability(DriverAvailability availability, string source, DateTimeOffset now) =>
        this with { Availability = availability, AvailabilitySource = source, SyncedAt = now };

    private DriverRestriction? ResolveRestriction(DriverStatus newStatus, DateOnly businessDate)
    {
        var required = DriverRestriction.For(newStatus, businessDate);
        if (required is null || newStatus != Status)
        {
            return required;
        }

        return Restriction ?? required;
    }
}

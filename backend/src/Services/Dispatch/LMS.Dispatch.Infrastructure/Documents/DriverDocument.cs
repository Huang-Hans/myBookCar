using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class DriverDocument
{
    [BsonId]
    public string DriverId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("licenseNumber")]
    public string LicenseNumber { get; init; } = string.Empty;

    // 业务日期以 YYYY-MM-DD 字符串存储，不转换成 UTC 午夜。
    [BsonElement("licenseExpiryDate")]
    public string LicenseExpiryDate { get; init; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; init; } = string.Empty;

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("assignedVehicleId")]
    [BsonIgnoreIfNull]
    public string? AssignedVehicleId { get; init; }

    [BsonElement("assignedVehicle")]
    [BsonDefaultValue("")]
    [BsonIgnoreIfDefault]
    public string AssignedVehicle { get; init; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("languages")]
    public List<string> Languages { get; init; } = [];

    [BsonElement("emergencyContact")]
    public EmergencyContactDocument EmergencyContact { get; init; } = new();

    [BsonElement("availability")]
    public string? Availability { get; init; }

    [BsonElement("availabilitySource")]
    public string? AvailabilitySource { get; init; }

    [BsonElement("syncedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? SyncedAt { get; init; }

    [BsonElement("restriction")]
    public DriverRestrictionDocument? Restriction { get; init; }

    // 本地登录凭据：带算法参数与盐的密码哈希，可空，缺字段按 null 读取。不属于司机领域模型，不进入任何 API 响应；
    // 业务更新整体替换文档时由仓储保留原值，因此领域模型和 FromDomain 都不接触它。
    [BsonElement("password")]
    [BsonIgnoreIfNull]
    public string? Password { get; set; }

    [BsonElement("deletedAt")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeletedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static DriverDocument FromDomain(Driver driver) => new()
    {
        DriverId = driver.DriverId,
        DeletedAt = driver.DeletedAt is null ? null : UtcDateTimes.ToStored(driver.DeletedAt.Value),
        Name = driver.Name,
        LicenseNumber = driver.LicenseNumber,
        LicenseExpiryDate = StoredValues.FromDate(driver.LicenseExpiryDate),
        Phone = driver.Phone,
        PropertyId = driver.PropertyId,
        AssignedVehicleId = driver.AssignedVehicleId,
        AssignedVehicle = driver.AssignedVehicleId is null ? driver.AssignedVehicle : string.Empty,
        Status = WireEnum.ToWire(driver.Status),
        Languages = driver.Languages.ToList(),
        EmergencyContact = EmergencyContactDocument.FromDomain(driver.EmergencyContact),
        Availability = driver.Availability is null ? null : WireEnum.ToWire(driver.Availability.Value),
        AvailabilitySource = driver.AvailabilitySource,
        SyncedAt = driver.SyncedAt is null ? null : UtcDateTimes.ToStored(driver.SyncedAt.Value),
        Restriction = driver.Restriction is null ? null : DriverRestrictionDocument.FromDomain(driver.Restriction),
        Version = driver.Version
    };

    public Driver ToDomain() => new(
        DriverId,
        Name,
        LicenseNumber,
        StoredValues.ToDate(LicenseExpiryDate, "licenseExpiryDate"),
        Phone,
        PropertyId,
        AssignedVehicle,
        StoredValues.ParseEnum<DriverStatus>(Status, "status"),
        Languages,
        EmergencyContact.ToDomain(),
        Availability is null ? null : StoredValues.ParseEnum<DriverAvailability>(Availability, "availability"),
        AvailabilitySource,
        SyncedAt is null ? null : UtcDateTimes.FromStored(SyncedAt.Value),
        Restriction?.ToDomain(),
        Version)
    {
        AssignedVehicleId = AssignedVehicleId,
        DeletedAt = DeletedAt is null ? null : UtcDateTimes.FromStored(DeletedAt.Value)
    };
}

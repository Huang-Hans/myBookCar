using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class ConfiguredPropertyDocument
{
    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; init; } = string.Empty;

    [BsonElement("contactPerson")]
    public string ContactPerson { get; init; } = string.Empty;

    [BsonElement("contactPhone")]
    public string ContactPhone { get; init; } = string.Empty;

    [BsonElement("assignedFleetCount")]
    public int AssignedFleetCount { get; init; }

    [BsonElement("operatingHours")]
    public string OperatingHours { get; init; } = string.Empty;

    [BsonElement("serviceLevel")]
    public string ServiceLevel { get; init; } = string.Empty;

    [BsonElement("minimumLeadTimeHours")]
    public int MinimumLeadTimeHours { get; init; }

    [BsonElement("cancellationPolicyReference")]
    public string CancellationPolicyReference { get; init; } = string.Empty;

    [BsonElement("membershipEligibility")]
    public string MembershipEligibility { get; init; } = string.Empty;

    [BsonElement("vipOverrideRule")]
    public string VipOverrideRule { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; init; }

    [BsonElement("version")]
    public long Version { get; init; } = 1;

    public ConfiguredProperty ToItem() => new(
        PropertyId, Name, Address, ContactPerson, ContactPhone, AssignedFleetCount, OperatingHours, ServiceLevel,
        MinimumLeadTimeHours, CancellationPolicyReference, MembershipEligibility, VipOverrideRule,
        IsActive, IsDeleted, Version);

    public static ConfiguredPropertyDocument FromItem(ConfiguredProperty item) => new()
    {
        PropertyId = item.PropertyId,
        Name = item.Name,
        Address = item.Address,
        ContactPerson = item.ContactPerson,
        ContactPhone = item.ContactPhone,
        AssignedFleetCount = item.AssignedFleetCount,
        OperatingHours = item.OperatingHours,
        ServiceLevel = item.ServiceLevel,
        MinimumLeadTimeHours = item.MinimumLeadTimeHours,
        CancellationPolicyReference = item.CancellationPolicyReference,
        MembershipEligibility = item.MembershipEligibility,
        VipOverrideRule = item.VipOverrideRule,
        IsActive = item.IsActive,
        IsDeleted = item.IsDeleted,
        Version = item.Version
    };
}

using LMS.Dispatch.Domain.Drivers;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

internal sealed class EmergencyContactDocument
{
    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; init; } = string.Empty;

    public static EmergencyContactDocument FromDomain(EmergencyContact contact) => new() { Name = contact.Name, Phone = contact.Phone };

    public EmergencyContact ToDomain() => new(Name, Phone);
}

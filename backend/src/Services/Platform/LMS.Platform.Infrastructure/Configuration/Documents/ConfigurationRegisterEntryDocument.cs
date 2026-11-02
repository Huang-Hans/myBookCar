using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class ConfigurationRegisterEntryDocument
{
    [BsonElement("areaCode")]
    public string AreaCode { get; init; } = string.Empty;

    [BsonElement("areaName")]
    public string AreaName { get; init; } = string.Empty;

    [BsonElement("mechanism")]
    public string Mechanism { get; init; } = string.Empty;

    [BsonElement("isEditable")]
    public bool IsEditable { get; init; }

    [BsonElement("owner")]
    public string Owner { get; init; } = string.Empty;

    [BsonElement("versionLabel")]
    public string VersionLabel { get; init; } = string.Empty;

    [BsonElement("sourceLabel")]
    public string SourceLabel { get; init; } = string.Empty;

    [BsonElement("modulePath")]
    public string ModulePath { get; init; } = string.Empty;

    public ConfigurationRegisterEntry ToItem() => new(
        AreaCode, AreaName, StoredValues.ParseEnum<ConfigurationMechanism>(Mechanism, "mechanism"), IsEditable, Owner, VersionLabel,
        SourceLabel, ModulePath);
}

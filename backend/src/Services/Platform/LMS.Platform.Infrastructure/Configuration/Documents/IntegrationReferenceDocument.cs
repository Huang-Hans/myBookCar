using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class IntegrationReferenceDocument
{
    [BsonElement("integrationCode")]
    public string IntegrationCode { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("configuredStatus")]
    public string ConfiguredStatus { get; init; } = string.Empty;

    [BsonElement("purpose")]
    public string Purpose { get; init; } = string.Empty;

    [BsonElement("configurationOwnership")]
    public string ConfigurationOwnership { get; init; } = string.Empty;

    [BsonElement("environmentLabel")]
    public string EnvironmentLabel { get; init; } = string.Empty;

    [BsonElement("dataSource")]
    public string DataSource { get; init; } = string.Empty;

    [BsonElement("accessPolicy")]
    public string AccessPolicy { get; init; } = string.Empty;

    // 分组只用于 group 查询过滤，不在对外视图中输出。
    [BsonElement("group")]
    public string Group { get; init; } = string.Empty;

    public IntegrationReference ToItem() => new(
        IntegrationCode, Name, ConfiguredStatus, Purpose, ConfigurationOwnership, EnvironmentLabel, DataSource, AccessPolicy,
        StoredValues.ParseEnum<IntegrationGroup>(Group, "group"));
}

using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Schedules;
using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Reporting.Infrastructure.Documents;

internal sealed class ReportScheduleDocument
{
    // 全物业范围在唯一键中的占位值；真实物业代码不会取这个值（物业代码由上传配置给出，不含星号）。
    public const string AllPropertiesKey = "*";

    [BsonId]
    public string ScheduleId { get; init; } = string.Empty;

    // 已规范化（去空白、小写）的邮箱，使“忽略大小写”的去重可以用普通唯一索引实现。
    [BsonElement("recipientEmail")]
    public string RecipientEmail { get; init; } = string.Empty;

    [BsonElement("frequency")]
    public string Frequency { get; init; } = string.Empty;

    [BsonElement("reportType")]
    public string ReportType { get; init; } = string.Empty;

    [BsonElement("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [BsonElement("isAllProperties")]
    public bool IsAllProperties { get; init; }

    [BsonElement("propertyScopes")]
    public List<string> PropertyScopes { get; init; } = [];

    // 设计扩展：登记范围键（全物业为 *，否则为物业代码），参与唯一索引与可见范围过滤。
    [BsonElement("scopeKey")]
    public string ScopeKey { get; init; } = string.Empty;

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; }

    [BsonElement("version")]
    public long Version { get; init; }

    public static ReportScheduleDocument FromDomain(ReportSchedule schedule) => new()
    {
        ScheduleId = schedule.ScheduleId,
        RecipientEmail = schedule.RecipientEmail,
        Frequency = schedule.Frequency,
        ReportType = WireEnum.ToWire(schedule.ReportType),
        CreatedBy = schedule.CreatedBy,
        IsAllProperties = schedule.Scope.IsAllProperties,
        PropertyScopes = schedule.Scope.IsAllProperties ? [] : [schedule.Scope.PropertyId!],
        ScopeKey = schedule.Scope.IsAllProperties ? AllPropertiesKey : schedule.Scope.PropertyId!,
        CreatedAt = UtcDateTimes.ToStored(schedule.CreatedAt),
        Version = schedule.Version
    };

    public ReportSchedule ToDomain() => new(
        ScheduleId,
        RecipientEmail,
        Frequency,
        StoredValues.ParseEnum<ReportType>(ReportType, "reportType"),
        CreatedBy,
        IsAllProperties ? PropertyScope.AllProperties : PropertyScope.ForProperty(PropertyScopes.Single()),
        UtcDateTimes.FromStored(CreatedAt),
        Version);
}

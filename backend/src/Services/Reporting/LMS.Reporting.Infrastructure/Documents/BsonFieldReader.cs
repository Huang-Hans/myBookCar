using LMS.Reporting.Infrastructure.Persistence;
using MongoDB.Bson;

namespace LMS.Reporting.Infrastructure.Documents;

// 从存储文档读取字段：必填字段缺失或类型不符是数据损坏（InvalidStoredDataException），可空字段缺失或为 null 返回 null。
internal sealed class BsonFieldReader(BsonDocument document, string context)
{
    public string Text(string name) =>
        Optional(name) is { IsString: true } value ? value.AsString : throw Invalid(name);

    public string? OptionalText(string name) =>
        Optional(name) is { } value ? (value.IsString ? value.AsString : throw Invalid(name)) : null;

    public int Int(string name) =>
        Optional(name) is { IsNumeric: true } value && value.ToInt64() is >= int.MinValue and <= int.MaxValue ? value.ToInt32() : throw Invalid(name);

    public long Long(string name) =>
        Optional(name) is { IsNumeric: true } value ? value.ToInt64() : throw Invalid(name);

    public long? OptionalLong(string name) =>
        Optional(name) is { } value ? (value.IsNumeric ? value.ToInt64() : throw Invalid(name)) : null;

    public double? OptionalDouble(string name) =>
        Optional(name) is { } value ? (value.IsNumeric ? value.ToDouble() : throw Invalid(name)) : null;

    public DateTimeOffset Instant(string name) =>
        Optional(name) is { IsValidDateTime: true } value ? UtcDateTimes.FromStored(value.ToUniversalTime()) : throw Invalid(name);

    public DateTimeOffset? OptionalInstant(string name) =>
        Optional(name) is { } value ? (value.IsValidDateTime ? UtcDateTimes.FromStored(value.ToUniversalTime()) : throw Invalid(name)) : null;

    // 缺失或为 null 时为 false。
    public bool OptionalFlag(string name) =>
        Optional(name) is { } value ? (value.IsBoolean ? value.AsBoolean : throw Invalid(name)) : false;

    public BsonFieldReader Child(string name) =>
        Optional(name) is { IsBsonDocument: true } value ? new BsonFieldReader(value.AsBsonDocument, $"{context}.{name}") : throw Invalid(name);

    private BsonValue? Optional(string name) =>
        document.TryGetValue(name, out var value) && !value.IsBsonNull ? value : null;

    private InvalidStoredDataException Invalid(string name) => new($"The stored field '{context}.{name}' is missing or has an unexpected type.");
}

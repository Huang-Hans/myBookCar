namespace LMS.BuildingBlocks.Core.Scoping;

// 调用者的物业数据范围，只来自持久化授权，不来自请求参数。
// IsAllProperties 为显式的全物业授权；否则只能访问 PropertyId 对应的单一物业。
public sealed record PropertyScope(bool IsAllProperties, string? PropertyId)
{
    public static PropertyScope AllProperties { get; } = new(true, null);

    public static PropertyScope ForProperty(string propertyId) => new(false, propertyId);

    public bool Allows(string propertyId) =>
        IsAllProperties || string.Equals(PropertyId, propertyId, StringComparison.Ordinal);
}

namespace LMS.BuildingBlocks.Core.Patching;

public static class ChangedFieldNames
{
    // 返回被设置（含显式 null）的字段名，用于审计的变更摘要。
    public static IReadOnlyList<string> From(params (string Name, bool IsSet)[] fields) =>
        fields.Where(field => field.IsSet).Select(field => field.Name).ToList();
}

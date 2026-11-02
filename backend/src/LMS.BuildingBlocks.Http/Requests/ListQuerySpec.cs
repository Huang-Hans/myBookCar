namespace LMS.BuildingBlocks.Http.Requests;

// 单个列表接口的排序约定：允许的排序字段、默认排序（逗号分隔，- 前缀为降序）以及末尾补充的稳定标识字段。
public sealed record ListQuerySpec(IReadOnlyList<string> SortFields, string DefaultSort, string StableField);

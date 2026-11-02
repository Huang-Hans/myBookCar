namespace LMS.Dispatch.Api.Contracts.Inspections;

/// <summary>检查项：Code 是稳定代码，Name 是默认英文显示名（可以变更，历史记录以代码为准）。</summary>
public sealed record InspectionItemView(string Code, string Name);

/// <summary>
/// 某阶段当前发布的检查模板。
/// </summary>
/// <param name="Phase">PRE_TRIP / POST_TRIP。</param>
/// <param name="Version">模板版本；提交时可原样带回，已提交的记录冻结提交时的版本。</param>
/// <param name="Items">检查项，全部必填。</param>
public sealed record InspectionTemplateView(string Phase, int Version, IReadOnlyList<InspectionItemView> Items);

namespace LMS.Dispatch.Domain.Inspections;

// 某阶段当前发布的检查模板。已提交的检查冻结了提交时的模板版本，模板后续变更不改写历史。
public sealed record InspectionTemplate(InspectionPhase Phase, int Version, IReadOnlyList<InspectionItem> Items);

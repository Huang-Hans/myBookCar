namespace LMS.BuildingBlocks.Http.Requests.Contracts;

// 标记允许空白的文本查询参数（例如 search、sort）：空白表示不筛选，而不是非法值。
[AttributeUsage(AttributeTargets.Property)]
public sealed class UnrestrictedTextAttribute : Attribute;

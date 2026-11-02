namespace LMS.BuildingBlocks.Core.Operations;

// 业务层发现的字段级问题，由接入层映射为 errors[]。Field 使用 camelCase 点号路径。
public sealed record FieldIssue(string Field, string Message);

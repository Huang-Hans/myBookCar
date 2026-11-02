namespace LMS.BuildingBlocks.Core.Querying;

// 排序项：Field 是对外 API 的排序字段名（已通过各接口的允许列表校验），不是数据库字段名。
public sealed record SortTerm(string Field, bool IsDescending);

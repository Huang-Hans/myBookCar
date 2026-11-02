namespace LMS.Dispatch.Domain.Inspections;

// 检查项：Code 是稳定代码，不随语言变化；Name 是默认英文显示名，可以变更而不影响历史记录。
public sealed record InspectionItem(string Code, string Name);

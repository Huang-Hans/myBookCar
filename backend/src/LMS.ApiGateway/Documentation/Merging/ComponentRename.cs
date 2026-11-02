namespace LMS.ApiGateway.Documentation.Merging;

// 组件重命名计划：同名但内容不同的组件加服务前缀，避免合并时互相覆盖。
internal sealed record ComponentRename(string Section, string OldName, string NewName)
{
    public string OldReference => $"#/components/{Section}/{OldName}";

    public string NewReference => $"#/components/{Section}/{NewName}";
}

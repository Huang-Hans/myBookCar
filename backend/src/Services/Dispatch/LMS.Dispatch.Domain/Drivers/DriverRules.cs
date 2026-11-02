using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Dispatch.Domain.Drivers;

// 合并后的完整司机必须满足的规则。
public static class DriverRules
{
    public static IReadOnlyList<FieldIssue> Validate(Driver driver)
    {
        var issues = new List<FieldIssue>();
        ValidateLanguages(driver.Languages, issues);
        if (string.IsNullOrWhiteSpace(driver.EmergencyContact.Name))
        {
            issues.Add(new FieldIssue("emergencyContact.name", "Must not be blank"));
        }

        if (string.IsNullOrWhiteSpace(driver.EmergencyContact.Phone))
        {
            issues.Add(new FieldIssue("emergencyContact.phone", "Must not be blank"));
        }

        return issues;
    }

    // 语言列表非空，元素非空白且不重复（不区分大小写）。
    private static void ValidateLanguages(IReadOnlyList<string> languages, List<FieldIssue> issues)
    {
        if (languages.Count == 0 || languages.Any(string.IsNullOrWhiteSpace))
        {
            issues.Add(new FieldIssue("languages", "Must contain at least one non-blank language"));
            return;
        }

        if (languages.Distinct(StringComparer.OrdinalIgnoreCase).Count() != languages.Count)
        {
            issues.Add(new FieldIssue("languages", "Must not contain duplicates"));
        }
    }
}

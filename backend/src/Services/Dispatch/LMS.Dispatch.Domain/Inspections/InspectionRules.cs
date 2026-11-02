using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Dispatch.Domain.Inspections;

// 提交检查的完整性规则：必填项全部出现，且没有重复项与未知项。结果为 FAIL 不是校验错误。
public static class InspectionRules
{
    public static IReadOnlyList<FieldIssue> Validate(InspectionTemplate template, IReadOnlyList<InspectionAnswer> answers)
    {
        var issues = new List<FieldIssue>();
        var known = template.Items.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);

        for (var index = 0; index < answers.Count; index++)
        {
            var code = answers[index].ItemCode;
            if (!known.Contains(code))
            {
                issues.Add(new FieldIssue($"answers[{index}].itemCode", "Unknown inspection item"));
            }
        }

        AddDuplicates(answers, issues);
        AddMissing(known, answers, issues);

        return issues;
    }

    private static void AddDuplicates(IReadOnlyList<InspectionAnswer> answers, List<FieldIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < answers.Count; index++)
        {
            if (!seen.Add(answers[index].ItemCode))
            {
                issues.Add(new FieldIssue($"answers[{index}].itemCode", "Duplicate inspection item"));
            }
        }
    }

    private static void AddMissing(HashSet<string> known, IReadOnlyList<InspectionAnswer> answers, List<FieldIssue> issues)
    {
        var answered = answers.Select(answer => answer.ItemCode).ToHashSet(StringComparer.Ordinal);
        foreach (var code in known.Where(code => !answered.Contains(code)))
        {
            issues.Add(new FieldIssue("answers", $"Missing inspection item {code}"));
        }
    }
}

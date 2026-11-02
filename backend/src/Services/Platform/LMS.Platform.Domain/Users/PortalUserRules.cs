using System.Net.Mail;
using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Platform.Domain.Users;

// 合并后的完整应用用户必须满足的规则。
public static class PortalUserRules
{
    public static IReadOnlyList<FieldIssue> Validate(PortalUser user)
    {
        var issues = new List<FieldIssue>();
        if (!IsEmailAddress(user.CorporateEmail))
        {
            issues.Add(new FieldIssue("corporateEmail", "Must be a valid email address"));
        }

        ValidatePropertyScope(user, issues);
        return issues;
    }

    // 全物业授权是显式选择：此时不得再指定单一物业；否则必须指定且只能指定一个物业。
    private static void ValidatePropertyScope(PortalUser user, List<FieldIssue> issues)
    {
        if (user.HasAllPropertyAccess && user.PropertyId is not null)
        {
            issues.Add(new FieldIssue("propertyId", "Must be null when hasAllPropertyAccess is true"));
        }

        if (!user.HasAllPropertyAccess && string.IsNullOrWhiteSpace(user.PropertyId))
        {
            issues.Add(new FieldIssue("propertyId", "Is required when hasAllPropertyAccess is false"));
        }
    }

    private static bool IsEmailAddress(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value && value.Contains('.', StringComparison.Ordinal);
}

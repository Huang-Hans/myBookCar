using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

// 等级卡片与矩阵不分页，也没有排序参数；定义只提供稳定代码，列表保持来源中的顺序。
public static class MembershipTierDefinition
{
    public static ConfigurationListDefinition<MembershipTier> Create() =>
        new(
            item => item.TierCode,
            new Dictionary<string, Func<MembershipTier, IComparable?>> { ["tierCode"] = item => item.TierCode },
            item => [item.TierCode, item.Name],
            "tierCode",
            "tierCode");
}

namespace LMS.Booking.Domain.Guests;

// 会员等级的本地排序（用于"最低可参与等级"等比较）。权威定义在 Platform 上传配置，此处只做序数比较。
public static class TierCodes
{
    public static readonly IReadOnlyList<string> Ascending = ["SILVER", "GOLD", "PLATINUM", "DIAMOND", "VIP"];

    // 未知等级返回 -1（视为不参与），调用方据此拒绝而不是猜测。
    public static int RankOf(string tierCode)
    {
        for (var index = 0; index < Ascending.Count; index++)
        {
            if (string.Equals(Ascending[index], tierCode, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
}

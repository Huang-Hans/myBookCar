namespace LMS.Booking.Application.Customers.Options;

// 收藏设置（Favorites 节）。每个分组的条目数上限只是防止异常请求的安全边界，不是已确认的业务规则（Q2-05）；
// 不继承员工收藏的 20 条上限。
public sealed class FavoritesOptions
{
    public const string SectionName = "Favorites";

    public int MaxItemsPerUsage { get; init; } = 50;
}

namespace LMS.Booking.Domain.Customers;

// 客户某一用途分组的收藏地点，键为 guestId + usage。保存语义是整体替换该分组（空数组即清空），顺序保留请求顺序。
// 从未保存过的分组读到空列表（版本 0），不写库。
public sealed record CustomerLocationFavorites(
    string GuestId,
    FavoriteUsage Usage,
    IReadOnlyList<LocationFavorite> Items,
    DateTimeOffset? UpdatedAt,
    long Version)
{
    private const long FirstVersion = 1;

    public static CustomerLocationFavorites Empty(string guestId, FavoriteUsage usage) => new(guestId, usage, [], null, 0);

    public static CustomerLocationFavorites Create(string guestId, FavoriteUsage usage, IReadOnlyList<LocationFavorite> items, DateTimeOffset now) =>
        new(guestId, usage, items, now, FirstVersion);

    public CustomerLocationFavorites Replace(IReadOnlyList<LocationFavorite> items, DateTimeOffset now) =>
        this with { Items = items, UpdatedAt = now };
}

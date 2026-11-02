namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>
/// 收藏的一个目录地点：保存时的名称与区域快照，以及当前可用性。
/// </summary>
/// <param name="LocationId">地点标识（目录引用）。</param>
/// <param name="Name">保存时的名称快照。</param>
/// <param name="Region">保存时的区域快照。</param>
/// <param name="IsAvailable">当前是否仍可用（启用）；false 表示已停用或已不存在（收藏保留，不静默删除）；这次无法读取目录时省略。创建订单仍会重新校验。</param>
public sealed record FavoriteLocationView(string LocationId, string Name, string Region, bool? IsAvailable);

/// <summary>
/// 某一用途分组的收藏。从未保存过为空列表（version=0），不写库。
/// </summary>
/// <param name="Usage">PICKUP / DROPOFF。</param>
/// <param name="Items">收藏项，顺序即保存顺序。</param>
/// <param name="UpdatedAt">最近保存时间（UTC）；从未保存过时省略。</param>
/// <param name="Version">乐观锁版本号；从未保存过为 0。</param>
public sealed record LocationFavoritesView(string Usage, IReadOnlyList<FavoriteLocationView> Items, DateTimeOffset? UpdatedAt, long Version);

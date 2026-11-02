namespace LMS.Booking.Api.Contracts.CustomerRecords;

/// <summary>一个待收藏的目录地点。</summary>
/// <param name="LocationId">Platform 目录地点标识；不接受任意文本冒充地点标识。</param>
public sealed record FavoriteItemRequest(string? LocationId);

/// <summary>
/// 整体替换某一用途分组的收藏。空数组清空该分组，另一分组不受影响；同一分组内不允许重复，同一地点可同时出现在两个分组。
/// 可选 X-Expected-Version：提供时必须等于当前版本（从未保存过为 0），否则 409。
/// </summary>
/// <param name="Items">收藏项，顺序即保存顺序。</param>
public sealed record ReplaceLocationFavoritesRequest(IReadOnlyList<FavoriteItemRequest>? Items);

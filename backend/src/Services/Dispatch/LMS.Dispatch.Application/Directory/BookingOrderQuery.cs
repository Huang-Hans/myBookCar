using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Directory;

// 派单队列列表查询。IncludeTripIds / ExcludeTripIds 由 Dispatch 按邀请状态算出；为 null 表示不按集合限定。
public sealed record BookingOrderQuery(
    ListQuery List,
    string? PropertyId,
    OrderChannel? Channel,
    DateTimeOffset? From,
    DateTimeOffset? To,
    BookingOrderGroup Group,
    IReadOnlyList<string>? IncludeTripIds,
    IReadOnlyList<string>? ExcludeTripIds);

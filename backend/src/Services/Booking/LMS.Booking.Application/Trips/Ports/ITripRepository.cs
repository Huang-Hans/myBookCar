using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Ports;

public interface ITripRepository
{
    Task<Trip?> FindByIdAsync(string tripId, CancellationToken cancellationToken);

    // 只返回对调用者物业范围可见的行程；不可见与不存在不作区分。
    Task<Trip?> FindVisibleAsync(string tripId, PropertyScope scope, CancellationToken cancellationToken);

    Task<Trip?> FindVisibleAsync(string tripId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<Trip>> ListAsync(ListTripsQuery query, CancellationToken cancellationToken);

    // 宾客最近的行程（档案页展示），按接车时间倒序。
    Task<IReadOnlyList<Trip>> ListRecentByGuestAsync(string guestId, PropertyScope scope, int take, CancellationToken cancellationToken);

    Task InsertAsync(Trip trip, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。
    Task<bool> TryReplaceAsync(Trip updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);

    // 生成当日的业务单号 TR-YYYYMMDD-NNN；序号取当日已建行程数 + 1，唯一性由 ux_trip_reference 保证。
    Task<string> NextReferenceAsync(DateOnly macauDate, ITransactionContext context, CancellationToken cancellationToken);

    // 当日（给定 UTC 窗口）非取消行程数：物业配额已用量的本服务权威口径（实施基线 D5）。
    Task<long> CountDailyActiveAsync(string propertyId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    // 各状态的行程数（状态汇总端点）。
    Task<IReadOnlyDictionary<TripStatus, long>> CountByStatusAsync(PropertyScope scope, CancellationToken cancellationToken);

    // 服务间状态推进：仅当当前状态等于 from 时原子更新为目标状态；返回 false 表示状态已被并发改变。
    Task<bool> TryAdvanceAsync(string tripId, TripStatus from, TripStatus to, CancellationToken cancellationToken);
}

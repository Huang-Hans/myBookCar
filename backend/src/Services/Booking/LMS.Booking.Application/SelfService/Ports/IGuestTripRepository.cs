using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Ports;

// 客户本人维度的订单读取。归属条件 guest.guestId（实际受益宾客）写在存储查询里，不用 bookedBy 判断归属：
// 客服代订给该宾客的订单同样属于该宾客。他人的订单与不存在不作区分。写入仍经 ITripRepository。
public interface IGuestTripRepository
{
    Task<PageResult<Trip>> ListForGuestAsync(ListGuestTripsQuery query, CancellationToken cancellationToken);

    Task<Trip?> FindForGuestAsync(string tripId, string guestId, CancellationToken cancellationToken);

    Task<Trip?> FindForGuestAsync(string tripId, string guestId, ITransactionContext context, CancellationToken cancellationToken);

    // 客户首页当前旅程：进行中（前往 / 到达 / 行程中）优先，其次最近的待出发行程。
    Task<Trip?> FindCurrentForGuestAsync(string guestId, DateTimeOffset now, CancellationToken cancellationToken);

    // 该客户在给定接车时间窗口内的非终态订单数，用于自助预订的每日活跃订单数上限。
    Task<long> CountActiveForGuestAsync(string guestId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);
}

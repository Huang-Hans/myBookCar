using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Offers.Models;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Ports;

public interface IDriverOfferRepository
{
    Task<DriverOffer?> FindByIdAsync(string offerId, CancellationToken cancellationToken);

    // 只返回属于该司机的邀请；他人的邀请与不存在不作区分。
    Task<DriverOffer?> FindForDriverAsync(string offerId, string driverId, CancellationToken cancellationToken);

    // 该司机仍有效的邀请：待响应且未过期，或处理中。按分页与排序返回。
    Task<PageResult<DriverOffer>> ListOpenForDriverAsync(string driverId, DateTimeOffset now, ListQuery list, CancellationToken cancellationToken);

    // 同一行程、同一司机是否已有占用中的邀请（待响应或处理中）。
    Task<bool> ExistsOpenAsync(string tripId, string driverId, CancellationToken cancellationToken);

    // 该行程最近一轮邀请（按创建时间）；没有为 null。
    Task<DriverOffer?> FindLatestForTripAsync(string tripId, CancellationToken cancellationToken);

    // 这些行程在 since 之后的最近一轮邀请；没有邀请的行程不出现在结果里。
    Task<IReadOnlyList<DriverOffer>> FindLatestForTripsAsync(IReadOnlyList<string> tripIds, DateTimeOffset since, CancellationToken cancellationToken);

    // 范围内每个行程最近一轮邀请的状态摘要（since 之后创建）；已被接受的邀请不返回，因为订单此时已有权威指派。
    Task<IReadOnlyList<OfferState>> ListLatestStatesAsync(PropertyScope scope, DateTimeOffset since, CancellationToken cancellationToken);

    // 行程占用中（待响应或处理中）的邀请，不论待响应的是否已过期。
    Task<IReadOnlyList<DriverOffer>> ListOpenForTripAsync(string tripId, CancellationToken cancellationToken);

    // 司机占用中（待响应或处理中）的邀请，不论待响应的是否已过期。
    Task<IReadOnlyList<DriverOffer>> ListHeldByDriverAsync(string driverId, CancellationToken cancellationToken);

    // 该物业内此刻被占用的司机：待响应且未过期，或处理中。
    Task<IReadOnlyList<string>> ListHeldDriverIdsAsync(string propertyId, DateTimeOffset now, CancellationToken cancellationToken);

    // 行程的邀请历史，按分页与排序返回。
    Task<PageResult<DriverOffer>> ListForTripAsync(string tripId, ListQuery list, CancellationToken cancellationToken);

    // 已超过有效期但仍是待响应的邀请，按过期时间从早到晚。
    Task<IReadOnlyList<DriverOffer>> ListExpiredPendingAsync(DateTimeOffset now, int take, CancellationToken cancellationToken);

    // 行程与司机各自至多有一张占用中的邀请，由存储的唯一约束保证；违反时抛出 DuplicateRecordException。
    Task InsertAsync(DriverOffer offer, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。
    Task<bool> TryReplaceAsync(DriverOffer updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> TryReplaceAsync(DriverOffer updated, long expectedVersion, CancellationToken cancellationToken);
}

using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Application.Guests.Ports;

namespace LMS.Booking.Api.Authorization;

// 客户令牌的 sub 必须对应 Booking 宾客表中仍存在的记录。司机令牌的 sub 对应 Dispatch 的司机记录，Booking 不拥有该主数据，
// 因此只信任已验签令牌；司机行程的归属由指派的 driverId 约束（仓储查询带 assignment.driverId），而不是由令牌之外的请求参数决定。
internal sealed class CustomerPrincipalRecordVerifier(IGuestDirectory guests) : IPrincipalRecordVerifier
{
    public async Task<bool> IsUsableAsync(PrincipalType type, string entityId, CancellationToken cancellationToken) =>
        type != PrincipalType.Customer || await guests.FindAsync(entityId, cancellationToken) is not null;
}

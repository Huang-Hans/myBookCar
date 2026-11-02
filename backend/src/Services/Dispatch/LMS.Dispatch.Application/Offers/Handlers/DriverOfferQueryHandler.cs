using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Handlers;

// 本人有效邀请。已过期但尚未被写回终态的邀请按时间过滤，不会出现在结果里。
internal sealed class DriverOfferQueryHandler(IDriverOfferRepository offers, TimeProvider timeProvider) : IDriverOfferQueryHandler
{
    public async Task<OperationResult<PageResult<DriverOffer>>> ListAsync(string driverId, ListQuery list, CancellationToken cancellationToken) =>
        OperationResult<PageResult<DriverOffer>>.Succeeded(
            await offers.ListOpenForDriverAsync(driverId, timeProvider.GetUtcNow(), list, cancellationToken));
}

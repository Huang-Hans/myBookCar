using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Offers.Options;
using LMS.Dispatch.Domain.Offers;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Offers.Services;

// 把已超过有效期的待响应邀请持久化为 EXPIRED，释放它占用的行程与司机。
// 司机端响应时仍按服务端时间判定，这里负责没有任何人再响应的邀请：后台定时清扫，并在重新派单前先关闭旧轮次。
internal sealed class OfferExpirationService(
    IDriverOfferRepository offers,
    OfferClosingService closing,
    IOptions<OfferOptions> options,
    TimeProvider timeProvider) : IOfferExpirationSweeper
{
    private const string SystemActor = "system:offer-expiry";

    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        var expired = await offers.ListExpiredPendingAsync(timeProvider.GetUtcNow(), options.Value.ExpiryBatchSize, cancellationToken);

        return await ExpireAsync(expired, cancellationToken);
    }

    // 只处理其中已过期的待响应邀请；处理中的邀请不会被当作过期。
    public Task<int> ExpireStaleAsync(IEnumerable<DriverOffer> candidates, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        return ExpireAsync(candidates.Where(offer => offer.IsExpiredAt(now)).ToList(), cancellationToken);
    }

    private async Task<int> ExpireAsync(IReadOnlyList<DriverOffer> expired, CancellationToken cancellationToken)
    {
        var context = new CommandContext(SystemActor, Guid.NewGuid().ToString("N"), PropertyScope.AllProperties);
        var closedCount = 0;
        foreach (var offer in expired)
        {
            var closed = await closing.CloseAsync(offer, offer.Expired(timeProvider.GetUtcNow()), context, cancellationToken);
            closedCount += closed ? 1 : 0;
        }

        return closedCount;
    }
}

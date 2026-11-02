using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Application.Offers.Models;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Offers.Services;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Handlers;

// 司机响应邀请。状态机本身幂等：重复提交同一决定得到同一结果，相反的决定或已关闭的邀请为状态冲突。
// 超时在响应时按服务端时间判定并写回终态，不依赖客户端计时器。
internal sealed class RespondToOfferHandler(
    IDriverOfferRepository offers,
    OfferAcceptanceService acceptance,
    OfferClosingService closing,
    TimeProvider timeProvider) : IRespondToOfferHandler
{
    public async Task<OperationResult<OfferResponse>> HandleAsync(RespondToOfferCommand command, CancellationToken cancellationToken)
    {
        var offer = await offers.FindForDriverAsync(command.OfferId, command.DriverId, cancellationToken);
        if (offer is null)
        {
            return OperationResult<OfferResponse>.Failed(OperationOutcome.NotFound);
        }

        var now = timeProvider.GetUtcNow();
        if (offer.IsExpiredAt(now))
        {
            await closing.CloseAsync(offer, offer.Expired(now), command.Context, cancellationToken);
            return OperationResult<OfferResponse>.Failed(OperationOutcome.StateConflict);
        }

        return command.Decision == OfferDecision.Accept
            ? await acceptance.AcceptAsync(offer, command, cancellationToken)
            : await DeclineAsync(offer, command, now, cancellationToken);
    }

    private async Task<OperationResult<OfferResponse>> DeclineAsync(
        DriverOffer offer,
        RespondToOfferCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (offer.Status == DriverOfferStatus.Declined)
        {
            return OperationResult<OfferResponse>.Succeeded(new OfferResponse(offer, false));
        }

        if (offer.Status != DriverOfferStatus.Pending)
        {
            return OperationResult<OfferResponse>.Failed(OperationOutcome.StateConflict);
        }

        var declined = offer.Declined(now);
        var closed = await closing.CloseAsync(offer, declined, command.Context, cancellationToken);

        return closed
            ? OperationResult<OfferResponse>.Succeeded(new OfferResponse(declined with { Version = offer.Version + 1 }, false))
            : OperationResult<OfferResponse>.Failed(OperationOutcome.StateConflict);
    }
}

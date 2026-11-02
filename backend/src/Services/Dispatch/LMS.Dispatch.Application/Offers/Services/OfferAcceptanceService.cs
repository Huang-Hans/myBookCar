using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Application.Offers.Models;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Services;

// 接受邀请：先校验司机资格与邀请状态，把邀请置为 ACCEPTING（持久化的处理中状态），再带稳定操作标识请求 Booking 条件指派。
// Booking 确认后才提交 ACCEPTED 与 ON_TRIP；Booking 暂时不可用时保持 ACCEPTING，司机可用同一邀请重试，不会产生第二笔指派。
internal sealed class OfferAcceptanceService(
    IDriverRepository drivers,
    IDriverOfferRepository offers,
    IBookingAssignments bookings,
    OfferAcceptanceCommitter committer,
    OfferClosingService closing,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<OfferResponse>> AcceptAsync(
        DriverOffer offer,
        RespondToOfferCommand command,
        CancellationToken cancellationToken)
    {
        if (offer.Status == DriverOfferStatus.Accepted)
        {
            return OperationResult<OfferResponse>.Succeeded(new OfferResponse(offer, false));
        }

        if (offer.Status is not (DriverOfferStatus.Pending or DriverOfferStatus.Accepting))
        {
            return OperationResult<OfferResponse>.Failed(OperationOutcome.StateConflict);
        }

        var driver = await drivers.FindByIdAsync(command.DriverId, cancellationToken);
        if (driver is null || !DriverSelfRules.CanAcceptOffers(driver))
        {
            return OperationResult<OfferResponse>.Rejected([new FieldIssue("driver", "Driver must be active and available to accept an offer")]);
        }

        var accepting = await EnsureAcceptingAsync(offer, cancellationToken);

        return accepting is null
            ? OperationResult<OfferResponse>.Failed(OperationOutcome.StateConflict)
            : await AssignAsync(accepting, driver, command, cancellationToken);
    }

    private async Task<DriverOffer?> EnsureAcceptingAsync(DriverOffer offer, CancellationToken cancellationToken)
    {
        if (offer.Status == DriverOfferStatus.Accepting)
        {
            return offer;
        }

        var accepting = offer.Accepting();

        return await offers.TryReplaceAsync(accepting, offer.Version, cancellationToken)
            ? accepting with { Version = offer.Version + 1 }
            : null;
    }

    private async Task<OperationResult<OfferResponse>> AssignAsync(
        DriverOffer accepting,
        Driver driver,
        RespondToOfferCommand command,
        CancellationToken cancellationToken)
    {
        var request = new AssignDriverRequest(
            accepting.TripId, accepting.OfferId, driver.DriverId, driver.Name, driver.AssignedVehicle, accepting.PropertyId);
        var assigned = await bookings.AssignDriverAsync(request, cancellationToken);

        return assigned.Outcome switch
        {
            BookingOutcome.Success => await CommitAsync(accepting, command, cancellationToken),
            BookingOutcome.Conflict or BookingOutcome.NotFound => await SupersedeAsync(accepting, command, cancellationToken),
            BookingOutcome.Forbidden => OperationResult<OfferResponse>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<OfferResponse>.Succeeded(new OfferResponse(accepting, true))
        };
    }

    private async Task<OperationResult<OfferResponse>> CommitAsync(
        DriverOffer accepting,
        RespondToOfferCommand command,
        CancellationToken cancellationToken)
    {
        if (await committer.CommitAsync(accepting, command.Context, cancellationToken))
        {
            return OperationResult<OfferResponse>.Succeeded(
                new OfferResponse(accepting.Accepted(timeProvider.GetUtcNow()) with { Version = accepting.Version + 1 }, false));
        }

        var current = await offers.FindForDriverAsync(accepting.OfferId, accepting.DriverId, cancellationToken);

        return current is { Status: DriverOfferStatus.Accepted }
            ? OperationResult<OfferResponse>.Succeeded(new OfferResponse(current, false))
            : OperationResult<OfferResponse>.Failed(OperationOutcome.StateConflict);
    }

    // 行程已不可分配（被他人指派、已取消、状态改变，或司机另有执行中的行程）：邀请失效，司机的可用状态不变。
    private async Task<OperationResult<OfferResponse>> SupersedeAsync(
        DriverOffer accepting,
        RespondToOfferCommand command,
        CancellationToken cancellationToken)
    {
        await closing.CloseAsync(accepting, accepting.Superseded(timeProvider.GetUtcNow()), command.Context, cancellationToken);

        return OperationResult<OfferResponse>.Failed(OperationOutcome.StateConflict);
    }
}

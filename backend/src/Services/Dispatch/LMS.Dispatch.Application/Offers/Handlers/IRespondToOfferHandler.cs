using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Application.Offers.Models;

namespace LMS.Dispatch.Application.Offers.Handlers;

public interface IRespondToOfferHandler
{
    Task<OperationResult<OfferResponse>> HandleAsync(RespondToOfferCommand command, CancellationToken cancellationToken);
}
